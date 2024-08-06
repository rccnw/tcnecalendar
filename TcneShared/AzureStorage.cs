using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.Text;
using Azure.Identity;
using TcneShared.Exceptions;
using TcneShared.Models;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;



namespace TcneShared
{
    public class AzureStorage
    {
        private static DateTimeOffset _lastRunTime = DateTime.MinValue;

        private readonly HttpClient _httpClient;
        private IConfiguration? _configuration;
        BlobServiceClient? _blobServiceClient;
        BlobContainerClient? _blobContainerClient;

        string? _accountName;
        string? _containerName;
        string? _blobName;
        string? _storageKey;
        string? _storageConnectionString;
        bool _updateStorageAccount;

        ILogger<AzureStorage>   _logger;
        CheckFrontApiService    _apiService;

        /// <summary>
        /// AzureStorage ctor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public AzureStorage(
            CheckFrontApiService    apiService,
            IHttpClientFactory      httpClientFactory,
            IConfiguration          configuration, 
            ILogger<AzureStorage>   logger)
        {
            _apiService     = apiService;
            _httpClient     = httpClientFactory.CreateClient();
            _configuration  = configuration;   
            _logger         = logger;

            _logger.LogInformation("AzureStorage ctor");

            _accountName            = string.Empty;
            _containerName          = string.Empty;
            _blobName               = string.Empty;
            _storageKey             = string.Empty;
            _storageConnectionString = string.Empty;
            _updateStorageAccount   = false;

            try
            {
                _accountName            = _configuration["AzureStorageAccountName"];
                _containerName          = _configuration["AzureStorageAccountContainerName"];
                _blobName               = _configuration["AzureStorageBlobName"];
                _storageKey             = _configuration["StorageKey"];
                _storageConnectionString = _configuration["StorageConnectionString"];
                _updateStorageAccount   = _configuration.GetValue<bool>("UpdateStorageAccount");

                _blobServiceClient      = new BlobServiceClient(_storageConnectionString);
                _blobContainerClient    = _blobServiceClient.GetBlobContainerClient(_containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }


        /// <summary>
        /// UpdateStorageFromCheckFront
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<int> UpdateStorageFromCheckFront()
        {
            int apiCallCount = 0;

            if (_logger is null)
            {
                throw new ArgumentNullException(nameof(_logger));
            }
            if (_apiService is null)
            {
                throw new ArgumentNullException(nameof(_apiService));
            }

            _logger.LogInformation("UpdateStorageFromCheckFront");

            string token = _apiService.GetBasicAuthToken();
            _logger.LogInformation($"Token: {token}");

            bool apiAvailable = await _apiService.PingCheckFrontApi();
            _logger.LogInformation($"CheckFront API available: {apiAvailable}");


            if (!apiAvailable)
            {
                // TODO: Add code to signal an alert for CheckFront service unavailability
                // await SendAlert();
                _logger.LogError($"PingCheckFrontApi failed : CheckFront API NOT AVAILABLE: {apiAvailable}");
                return apiCallCount;
            }

            try
            {
                List<SchedulerAppointmentData> listAppointments = new();
                listAppointments = await GetAppointments();                 // fetch from CheckFront API
                apiCallCount = listAppointments.Count;
                await SaveAppointmentsAzure(listAppointments);              // save to Azure Storage
                return apiCallCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateStorageFromCheckFront failed  - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// GetAppointments
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="Configuration"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<SchedulerAppointmentData>> GetAppointments()
        {
            _logger.LogInformation("GetAppointments");

            List<SchedulerAppointmentData> listAppointments = new List<SchedulerAppointmentData>();


            #region PingCheckFrontApi

            if (_apiService != null)
            {
                bool apiReady = await _apiService.PingCheckFrontApi();  // ensure CheckFront API is available
                if (!apiReady)
                {
                    _logger.LogError("GetAppointments:  PingCheckFrontApi false - CheckFront service is not responding");
                    throw new Exception();  // TODO custom exception error indicator and alert
                }
            }
            else
            {
                // Handle the case when _apiService is null
                _logger.LogError("GetAppointments:  _apiService is null");
                throw new Exception();
            }
            #endregion


            if (_configuration == null)
            {
                _logger.LogError("GetAppointments:  _configuration is null");
                throw new Exception();
            }

            try
            {
                var apiUrl = _configuration["CheckFront_Api_Url"];
                var limit = _configuration["CheckFrontApiLimit"];

                // use query param to set limit of records returned (CheckFront default is 100, max seems to be 250)
                // The results are paged, so need to know the pages value, and then make multiple calls to get all the data
                string bookingUrl = apiUrl + $"?limit=" + limit;    

                // https://tcne.checkfront.com/api/3.0/booking?limit=250&page=2


                // Get a new list of appointments from CheckFront
                var token = _apiService.GetBasicAuthToken();
                string bookingJson = await _apiService.GetJsonCheckFrontApiAsync(bookingUrl, token);



                Root? bookingModel = JsonSerializer.Deserialize<Root>(bookingJson);
                int pages = bookingModel.Request.Pages;
                int page  = bookingModel.Request.Page;

                if (pages > 1)
                {
                    // need to get the rest of the pages
                    for (int i = 2; i <= pages; i++)
                    {
                        string pageUrl = apiUrl + $"?limit=" + limit + "&page=" + i;
                        string pageJson = await _apiService.GetJsonCheckFrontApiAsync(pageUrl, token);
                        Root? pageModel = JsonSerializer.Deserialize<Root>(pageJson);

                        if (pageModel != null)
                        {
                            foreach (var kvp in pageModel.BookingIndex)
                            {
                                bookingModel.BookingIndex.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }

                int numBookings = 0;
                if (bookingModel != null)
                {
                    // now we have the list of bookings  from CheckFront, we need to get the detail for each booking and merge it into the list
                    // first filter these results to only include bookings for today or future dates

                    try
                    {
                        Debug.WriteLine($"BookingIds: --------------------------------");

                        var futureValidBookings = new List<Booking>();

                        foreach (KeyValuePair<string, Booking> booking in bookingModel.BookingIndex)
                        {

                            numBookings++;

                            Debug.WriteLine($"BookingId: {booking.Value.BookingId}");

                            //if ((booking.Value.BookingId == 309) || (booking.Value.BookingId == 313))
                            //{
                            //    Debug.WriteLine($"BookingId: {booking.Value.BookingId}");
                            //}

                            //ignore some bookings, no need to get detail for them
                            if (booking.Value.StatusName == "Cancelled") { continue; }
                            if (booking.Value.StatusName == "Void")      { continue; }


                            //ignore bookings in the past. Note we haven't fetched the detail info, so using the parent Booking model to check the date
                            string dateString   = booking.Value.DateDescription;
                            DateTime parsedDate = DateTime.MinValue;

                            TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");             
                            DateTime dateTimeHere = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), pacificZone);

                            DateTime today = dateTimeHere.Date;

                            DateTime yesterday = today.Date.AddMinutes(-5);             // today.AddDays(-1).Date;

                            // since the date string can have two forms depending on the date, either one or two characters,
                            // may need to parse both forms to detect
                            if (DateTime.TryParseExact(dateString, "ddd MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            {
                                if (parsedDate > yesterday)
                                {
                                    futureValidBookings.Add(booking.Value);    
                                }
                                continue;
                            }
                            else if (DateTime.TryParseExact(dateString, "ddd MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            {
                                // handle dates with single digit day
                                if (parsedDate > yesterday)
                                {
                                    futureValidBookings.Add(booking.Value);
                                }
                                continue;
                            }
                            else
                            {
                                // Handle unusual circumstances where the dateString is not in the format expected above.
                                // This can happen when a booking spans days:   'Tue Mar 26 2024 - Tue Apr 2 2024'   - 2 dates in the string
                                // NOTE that in this case there is NO COMMA after the date field, unlike the other date format strings

                                // since we are filtering past bookings here, use the second date dectected in this string for comparison
                                // This might produce a situation where an booking spans past and present/future days, in which case
                                // we will display the entire range of days (presumed, not tested)

                                // split a string in this format into its components: 'Tue Mar 26 2024 - Tue Apr 2 2024'
                                string[] parts = dateString.Split('-');
                                string firstString = parts[0].Trim();
                                string secondString = parts[1].Trim();


                                // examine the second date in the string to see if it is in the future and should be included
                                if (DateTime.TryParseExact(secondString, "ddd MMM dd yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                                {
                                    if (parsedDate > yesterday)
                                    {
                                        futureValidBookings.Add(booking.Value);
                                    }
                                    continue;
                                }
                                else if (DateTime.TryParseExact(secondString, "ddd MMM d yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                                {
                                    // handle dates with single digit day
                                    if (parsedDate > yesterday)
                                    {
                                        futureValidBookings.Add(booking.Value);
                                    }
                                    continue;
                                }
                                else
                                {
                                    // in any case assume it is a valid booking record, just let it through
                                    // But if this is in the past it will be displayed in the scheduler as a past booking 

                                    //_logger.LogWarning($"GetAppointments:  Unabled to parse date : '{dateString}'");

                                    // none of the checks for bookings prior to day have detected a prior booking,
                                    // so this item is considered a valid item to be displayed.
                                    // If this is a situation where the booking spans days, each day will be an distinct item to be added.
                                    futureValidBookings.Add(booking.Value);
                                }
                            }
                        }

                        Debug.WriteLine($"numBookings = {numBookings}");
                        Debug.WriteLine($" --------------------------------");

                        // because some bookings can span days, the futureValidBookings list may contain multiple bookings for the same bookingId.
                        // The detailModel.BookingDetail.Items will contain the detail for each day of the booking.
                        // In that case, additional bookings will be created for each day of the booking.
                        // The listDisplayAppointments list will contain the final list of appointments to display in the scheduler,
                        // including any virtual bookings created for multi-day bookings.

                        List<Booking> listDisplayBookings = new();

                        foreach (var booking in futureValidBookings)
                        {


                            //if ((booking.BookingId == 309) || (booking.BookingId == 313) )    
                            //{
                            //    Debug.WriteLine($"BookingId: {booking.BookingId}");
                            //}


                            string detailUrl = apiUrl + "/" + booking.BookingId.ToString();

                            // this will get the detail for a single booking.
                            // using ConfigureAwait means don't need to resume execution on the original context
                            string jsonDetail = await _apiService.GetJsonCheckFrontApiAsync(detailUrl, token).ConfigureAwait(false);

                            CheckFrontBookingDetail.RootObject? detailModel = new CheckFrontBookingDetail.RootObject();

                            try
                            {
                                detailModel = JsonSerializer.Deserialize<CheckFrontBookingDetail.RootObject>(jsonDetail);
                            }
                            catch (JsonException ex)
                            {
                                // Log the exception and the problematic item
                                _logger.LogError($"Error deserializing item for booking Id: {booking.BookingId}");
                                _logger.LogError($"Exception Message: {ex.Message}");
                                // Optionally, log additional details from the exception
                                _logger.LogError($"{ex.Data}");
                                _logger.LogError($"{ex.InnerException}");

                                // for some reason, this item's json can't be deserialized
                                // This means there are 3 important unknowns:
                                // 1) start time
                                // 2) end time of the rental
                                // 3) which space Nest/Hideout 
                            }

                            if ((detailModel != null) && (detailModel.BookingDetail != null) && (detailModel.BookingDetail.Items != null))
                            {
                                int itemNo = 0;
                                try
                                {
                                    foreach (KeyValuePair<string, CheckFrontBookingDetail.Item> detailItem in detailModel.BookingDetail.Items)
                                    {
                                        Booking vBooking    = new(booking);
                                        vBooking.Studio     = detailItem.Value.Studio;
                                        vBooking.StartDate  = detailItem.Value.StartDate;
                                        vBooking.EndDate    = detailItem.Value.EndDate;


                                        bool result = Is24HourBooking(detailItem.Value.StartDate, detailItem.Value.EndDate);
                                        if (result)
                                        {
                                            var newEndDate = detailItem.Value.EndDate - 300;  // subtract 5 minutes from the end date

                                            vBooking.EndDate = newEndDate;
                                        }

                                        itemNo++;
        
                                        listDisplayBookings.Add(vBooking);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"BookingId {booking.BookingId} error processing detail items -  itemNo: {itemNo}");
                                    _logger.LogError($"{ex.Message}");
                                }
                            }
                            else
                            {
                                _logger.LogError($"BookingId {booking.BookingId}  detailModel is null");
                            }

                            string? value = _configuration["ApiRateLimitDelayMilliseconds"];
                            if (value == null) { value = "500";  }  // default if config fails  
                            int apiRateLimitDelay = int.Parse(value);
                            _logger.LogInformation($"Thottle API calls  : {apiRateLimitDelay} ms");
                            Task.Delay(apiRateLimitDelay).Wait();  // delay to avoid CheckFront API rate limit   
                        }

                        listAppointments = ConvertModelToApptData(listDisplayBookings); 
                    }
                    catch (Exception)
                    {
                        _logger.LogError("GetAppointments:  exception Processing bookingModel");
                        throw;
                    }
                }
                return listAppointments;
            } 
            catch (Exception ex)
            {
                _logger.LogError($"GetAppointments:  exception   {ex.Message}");
                throw;
            }
        }

        private bool Is24HourBooking(long startDate, long endDate)
        {
            var startDateTime = UnixTimeStampToDateTime(startDate);
            var endDateTime = UnixTimeStampToDateTime(endDate);

            if (startDateTime.Hour == 0 && startDateTime.Minute == 0 && endDateTime.Hour == 0 && endDateTime.Minute == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertModelToApptData
        /// </summary>
        /// <param name="modelData"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        private List<SchedulerAppointmentData> ConvertModelToApptData(List<Booking> modelData) // , string displayLocation)
        {
            _logger.LogInformation("ConvertModelToApptData");

            List<SchedulerAppointmentData> appointmentData = new List<SchedulerAppointmentData>();

            try
            {
                var nestId      = _configuration["CheckFront_Category_Nest"];
                var hideoutId   = _configuration["CheckFront_Category_Hideout"];

                int id = 1;
                foreach (Booking booking in modelData)
                {
                    //ignore some bookings, no need to get detail for them
                    if (booking.StatusName == "Cancelled")  { continue; }
                    if (booking.StatusName == "Void")       { continue; }

                    Debug.WriteLine($"StatusName:  {booking.StatusName}");

                    var startDateTime = UnixTimeStampToDateTime(booking.StartDate);
                    var endDateTime   = UnixTimeStampToDateTime(booking.EndDate);

                    string location = "Reserved";
                    string css      = "unknown";

                    if (booking.Studio.ToString() == nestId)
                    {
                        css      = "nest"; 
                        location = "Nest";
                    }
                    else if (booking.Studio.ToString() == hideoutId)
                    {
                        css      = "hideout";  
                        location = "Hideout";
                    }
                    string subject = "RENTED";

                    if (booking.StatusName.ToLower() == "cleaning")
                    {
                        subject = "CLEANING";
                    }

                    string endTime = endDateTime.ToString("hh:mm tt");

                    // add a SyncFusion SchedulerAppointmentData object to the collection
                    appointmentData.Add(new SchedulerAppointmentData
                    {
                        Id          = id,
                        Subject     = subject + " Ends: " + endTime,
                        Location    = location,
                        StartTime   = startDateTime,
                        EndTime     = endDateTime,
                        CssClass    = css
                    });

                    id++;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("ConvertModelToApptData  ---  ERROR ---------");

            }
            return appointmentData;
        }

        //   Epoch is an instant of time considered to be the starting point for a particular period.
        //   The Unix time started on 1970-01-01 00:00:00 GMT, and stored as an integer representing seconds past this "epoch" time.
        //   
        public DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Convert Unix timestamp to DateTime in UTC
            DateTime utcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp);

            // Convert UTC DateTime to Pacific Time
            TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime pacificDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, pacificZone);

            // Check if Pacific Time is currently observing Daylight Saving Time
            //bool isDaylightSavingTime = pacificZone.IsDaylightSavingTime(pacificDateTime);
            //if (isDaylightSavingTime)
            //{
            //    pacificDateTime = pacificDateTime.AddHours(-1);
            //}

            return pacificDateTime;
        }



        /// <summary>
        /// SaveAppointmentsAzure
        /// </summary>
        /// <param name="listAppointments"></param>
        /// <returns></returns>

        public async Task SaveAppointmentsAzure(List<SchedulerAppointmentData> listAppointments)
        {
             _logger.LogInformation("SaveAppointmentsAzure");

            try
            {
                await _blobContainerClient!.CreateIfNotExistsAsync();

                string jsonString = JsonSerializer.Serialize<List<SchedulerAppointmentData>>(listAppointments);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

                var blobClient = _blobContainerClient.GetBlobClient(_blobName);

                // Check if the blob exists
                if (!await blobClient.ExistsAsync())
                {

                    // Blob does not exist, handle the situation accordingly
                    _logger.LogInformation("Blob does not exist.");
                     await blobClient.UploadAsync(new MemoryStream());  // an empty stream will create the blob
                }

                await blobClient.UploadAsync(stream, true);  // this will replace any existing data in the blob
            }
            catch (CredentialUnavailableException ex)
            {
                _logger.LogInformation(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// LoadAppointmentsAzure
        /// </summary>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>

        public async Task<List<SchedulerAppointmentData>> LoadAppointmentsAzure(string displayLocation)
        {
            _logger.LogInformation("LoadAppointmentsAzure");

            string data = string.Empty;
            string blobContents = string.Empty;
            try
            {
                var blobClient = _blobContainerClient!.GetBlobClient(_blobName);

                if (!await blobClient.ExistsAsync())
                {
                    // Blob does not exist, handle the situation accordingly
                    _logger.LogInformation($"Blob does not exist. _blobName = '{_blobName}'");
                    throw new NotImplementedException();
                }

                BlobDownloadResult result = await blobClient.DownloadContentAsync();
                blobContents = result.Content.ToString();
            }
            catch (CredentialUnavailableException ex)
            {
                _logger.LogInformation(ex.Message);
                   throw;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw;
            }

            try
            {
                if (!string.IsNullOrEmpty(blobContents))
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(blobContents);
                    using (MemoryStream stream = new MemoryStream(byteArray))
                    {
                        List<SchedulerAppointmentData>? list = await JsonSerializer.DeserializeAsync<List<SchedulerAppointmentData>>(stream);
                        if (list != null)
                        {
                            return FilterByDisplayLocation(list, displayLocation);
                        }
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
            }
            return new List<SchedulerAppointmentData>();
        }

        /// <summary>
        /// FilterByDisplayLocation
        /// </summary>
        /// <param name="list"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        private List<SchedulerAppointmentData> FilterByDisplayLocation(List<SchedulerAppointmentData> list, string displayLocation)
        {
            _logger.LogInformation("FilterByDisplayLocation");

            List<SchedulerAppointmentData> filteredList = new List<SchedulerAppointmentData>();

            foreach (var item in list)
            {
                if (displayLocation == "all")
                {
                    item.Location = string.Empty;   // we only need this to filter by location, don't need to display it.
                    filteredList.Add(item);
                }
                else if (item.Location.ToLower() == displayLocation)
                {
                    item.Location = string.Empty;   // we only need this to filter by location, don't need to display it.
                    filteredList.Add(item);
                }
                // else ignore this item
            }
            return filteredList;
        }

        public async Task SetWebhookRunTime()
        {
            // Convert UTC DateTime to Pacific Time
            TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime pacificDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pacificZone);

            TableClient tableClient = await GetTableClient();
            var entity = new TableEntity("Webhook", "LastRun")
            {
                { "Timestamp", pacificDateTime }
            };
            _lastRunTime = pacificDateTime;


            // Check if the entity already exists
            var existingEntity = await tableClient.GetEntityAsync<TableEntity>("Webhook", "LastRun");
            if (existingEntity == null)
            {
                await tableClient.AddEntityAsync(entity, CancellationToken.None);
            }
            else
            {
                entity.ETag = existingEntity.Value.ETag;
                await tableClient.UpdateEntityAsync(entity, new Azure.ETag(existingEntity.Value.ETag.ToString()), TableUpdateMode.Replace);
            }
        }


        private async Task<TableClient> GetTableClient()
        {
            if (_configuration is null)
            {
                throw new ArgumentNullException(nameof(_configuration));
            }
            var tableName = _configuration["WebHookTableName"];

            if (String.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            var tableServiceClient = new TableServiceClient(_storageConnectionString);

            var tableClient = tableServiceClient.GetTableClient(tableName);

            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        public async Task<DateTimeOffset?> GetLastWebhookRunTime()
        {
            if (_configuration is null)
            {
                throw new ArgumentNullException(nameof(_configuration));
            }   

            DateTimeOffset? dtLastWebHookRun = DateTime.MinValue;
;
            var tableClient = await GetTableClient();
            var entity = tableClient.GetEntity<TableEntity>("Webhook", "LastRun");

            dtLastWebHookRun = entity.Value.Timestamp;

            return dtLastWebHookRun;
        }
    }
}
