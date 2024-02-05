using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.Text;
using Azure.Identity;
using TcneShared.Models;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TcneShared;


namespace TcneShared
{

    /// <summary>
    /// storage account name:   tcne
    /// container name:  appointments
    ///   "AzureStorageAccountName" :  "tcne",
    /// AzureStorageAccountContainerName":  "appointments"
    /// https://github.com/Azure-Samples/ms-identity-easyauth-dotnet-storage-graphapi/tree/main/1-WebApp-storage-managed-identity
    /// 
    /// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-download
    /// 
    /// https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#install-azurite
    /// 
    /// </summary>

    public class AzureStorage
    {
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
        public async Task UpdateStorageFromCheckFront()
        {
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
                return;
            }

            try
            {
                List<SchedulerAppointmentData> listAppointments = new();
                listAppointments = await GetAppointments();                 // fetch from CheckFront API
                await SaveAppointmentsAzure(listAppointments);              // save to Azure Storage
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
                    throw new Exception();
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

                string bookingUrl = apiUrl + $"?limit=" + limit;    // use query param to set limit of records returned (CheckFront default is 100)

                // Get a new list of appointments from CheckFront
                var token = _apiService.GetBasicAuthToken();
                string bookingJson = await _apiService.GetJsonCheckFrontApiAsync(bookingUrl, token);

                Root? bookingModel = JsonSerializer.Deserialize<Root>(bookingJson);

                if (bookingModel != null)
                {
                    // now we have the list of bookings  from CheckFront, we need to get the detail for each booking and merge it into the list
                    // first filter these results to only include bookings for today or future dates

                    try
                    {
                        var futureValidBookings = new List<Booking>();
                        foreach (KeyValuePair<string, Booking> booking in bookingModel.BookingIndex)
                        {
                            //ignore some bookings, no need to get detail for them
                            if (booking.Value.StatusName == "Cancelled") { continue; }
                            if (booking.Value.StatusName == "Void") { continue; }


                            //ignore bookings in the past. Note we haven't fetched the detail info, so using the parent Booking model to check the date
                            string dateString   = booking.Value.DateDescription;
                            DateTime parsedDate = DateTime.MinValue;

                            if (DateTime.TryParseExact(dateString, "ddd MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            {
                                if (parsedDate >= DateTime.Now)
                                {
                                    futureValidBookings.Add(booking.Value);
                                    continue;
                                }
                            }
                            else if (DateTime.TryParseExact(dateString, "ddd MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            {
                                // handle dates with single digit day
                                if (parsedDate >= DateTime.Now)
                                {
                                    futureValidBookings.Add(booking.Value);
                                    continue;
                                }
                            }
                            else
                            {
                                // no clue, just let it through
                                _logger.LogError($"GetAppointments:  Error parsing date : '{dateString}'");
                                futureValidBookings.Add(booking.Value); // go ahead and add it to the list anyway
                            }
                        }

                        foreach (var booking in futureValidBookings)
                        {
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
                                    long finalEndDate = 0; // this is the maximum timestamp found in the items collection.

                                    // usually a booking detail will only have a single item in this collection.
                                    // in the event there are 2 or more, then the requirement is to compute the total duration of the booking.
                                    // to do that, iterate the items collection and capture the latest enddate and use that for the enddate of the booking

                                    foreach (KeyValuePair<string, CheckFrontBookingDetail.Item> detailItem in detailModel.BookingDetail.Items)
                                    {
                                        booking.Studio      = detailItem.Value.Studio;
                                        booking.EndDate     = detailItem.Value.EndDate;
                                        booking.StartDate   = detailItem.Value.StartDate;

                                        if (detailItem.Value.EndDate > finalEndDate)
                                        {
                                            finalEndDate = detailItem.Value.EndDate;
                                        }
                                        itemNo++;
                                    }

                                    booking.EndDate = finalEndDate;
                                    _logger.LogInformation($"BookingId {booking.BookingId}   Detail Items count : {itemNo}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"BookingId {booking.BookingId} error processing detail items -  itemNo: {itemNo}");
                                    _logger.LogError($"{ex.Message}");
                                }
                            }

                            // booking SHOULD be the booking model that has StartDate, EndDate, Studio
                            Debug.WriteLine("---");
                            Task.Delay(1000).Wait();  // delay to avoid CheckFront API rate limit
                        }
                        listAppointments = ConvertModelToApptData(futureValidBookings); //, displayLocation);
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
                int id = 1;
                foreach (Booking booking in modelData)
                {
                    //ignore some bookings, no need to get detail for them
                    if (booking.StatusName == "Cancelled")  { continue; }
                    if (booking.StatusName == "Void")       { continue; }

                    Debug.WriteLine($"StatusName:  {booking.StatusName}");

                    // Access the key and value of each item in the dictionary
                    //var key = item.Key;
                    //var booking = item.Value;

                    Debug.WriteLine($"BookingId       : {booking.BookingId}");
                    Debug.WriteLine($"Summary         : {booking.Summary}");
                    Debug.WriteLine($"StatusId        : {booking.StatusId}");
                    Debug.WriteLine($"StatusName      : {booking.StatusName}");
                    Debug.WriteLine($"CustomerName    : {booking.CustomerName}");
                    Debug.WriteLine($"DateDescription : {booking.DateDescription}");
                    Debug.WriteLine("===================================");

                    var startDateTime = UnixTimeStampToDateTime(booking.StartDate);
                    var endDateTime   = UnixTimeStampToDateTime(booking.EndDate);

                    string location = "Reserved";
                    string css      = "unknown";

                    if (booking.Studio == 13)
                    {
                        css      = "nest"; // #1861ac;  e-appointment.
                        location = "Nest";
                    }
                    else if (booking.Studio == 14)
                    {
                        css      = "hideout";    // e-appointment.
                        location = "Hideout";
                    }

                    string todStart = startDateTime.ToShortTimeString();
                    string todEnd   = endDateTime.ToShortTimeString();

                    //string subject = $"{location} {todStart} - {todEnd}";
                    string subject = $"-{todEnd}"; // this is a hack to get the scheduler to display the end time in the subject field

                    // add a SyncFusion AppointmentData object to the collection
                    appointmentData.Add(new SchedulerAppointmentData
                    {
                        Id = id,
                        Subject     = booking.StatusName,
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
                    filteredList.Add(item);
                }
                else if (item.Location.ToLower() == displayLocation)
                {
                    filteredList.Add(item);
                }
            }
            return filteredList;
        }
    }
}
