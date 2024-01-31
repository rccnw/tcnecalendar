using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.Text;
using Azure.Identity;
using TcneCalendar.Models;
using static TcneCalendar.Models.CheckFrontBookingDetail;
using System.Globalization;


namespace TcneCalendar
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

    public static class AzureStorage
    {
        static BlobServiceClient _blobServiceClient;
        static BlobContainerClient _blobContainerClient;


        static string? _accountName = string.Empty;
        static string? _containerName = string.Empty;
        static string? _blobName = string.Empty;
        static string? _storageKey = string.Empty;
        static string? _storageConnectionString = string.Empty;
        static bool    _updateStorageAccount = false;


        //        static ILogger<AzureStorage> _logger;

        //static AzureStorage(IConfiguration Configuration) 
        //{ 

        //}

        /// <summary>
        /// InitBlobServiceClient
        /// </summary>
        /// <param name="Configuration"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void InitBlobServiceClient(IConfiguration Configuration)  // , ILogger<AzureStorage> logger
        {
            // var credential = new DefaultAzureCredential();  this was for MSI

            if (Configuration == null)
            {
                throw new ArgumentNullException(nameof(Configuration));
            }

            _accountName = Configuration["AzureStorageAccountName"];   

            _containerName          = Configuration["AzureStorageAccountContainerName"];
            _blobName               = Configuration["AzureStorageBlobName"];
            _storageKey             = Configuration["StorageKey"];
            _storageConnectionString = Configuration["StorageConnectionString"];

            _updateStorageAccount = Configuration.GetValue<bool>("UpdateStorageAccount");

            _blobServiceClient = new BlobServiceClient(_storageConnectionString);
            _blobContainerClient    = _blobServiceClient.GetBlobContainerClient(_containerName);
        }

        /// <summary>
        /// UpdateStorageAccount
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="Configuration"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        public static async Task<List<SchedulerAppointmentData>> UpdateStorageAccount(HttpClient httpClient, IConfiguration Configuration, string displayLocation)
        {
            List<SchedulerAppointmentData> listAppointments = new List<SchedulerAppointmentData>();

            if (_updateStorageAccount)
            {
                listAppointments = await GetAppointments(httpClient, Configuration, displayLocation);
                Debug.WriteLine($"listAppointments.Count: {listAppointments.Count}");
                await SaveAppointmentsAzure(listAppointments);
            }
            // in any case, fetch the list of appointments from Azure Storage
            listAppointments = await AzureStorage.LoadAppointmentsAzure(displayLocation);
            return listAppointments;
        }



        /// <summary>
        /// GetAppointments
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="Configuration"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async static Task<List<SchedulerAppointmentData>> GetAppointments(HttpClient httpClient, IConfiguration Configuration, string displayLocation)
        {
            List<SchedulerAppointmentData> listAppointments = new List<SchedulerAppointmentData>();
            var service = new CheckFrontApiService(httpClient, Configuration);
            // ensure CheckFront API is available
    

            bool apiReady = await service.PingCheckFrontApi();
            if (!apiReady) { throw new Exception(); }       // TODO this needs to be handled better - display in UI and send email


            var apiUrl = Configuration["CheckFront_Api_Url"];
            var limit = Configuration["CheckFrontApiLimit"];

            string bookingUrl = apiUrl + $"?limit=" + limit;    // use query param to set limit of records returned (CheckFront default is 100)



            // Get a new list of appointments from CheckFront
            var token = service.GetBasicAuthToken();
            string bookingJson = await service.GetJsonCheckFrontApiAsync(bookingUrl, token);

            Root? bookingModel = JsonSerializer.Deserialize<Root>(bookingJson);

            if (bookingModel != null)
            {
                // now we have the list of bookings  from CheckFront, we need to get the detail for each booking and merge it into the list
                // first filter these results to only include bookings for today or future dates

                var futureValidBookings = new List<Booking>();
                foreach (KeyValuePair<string, Booking> booking in bookingModel.BookingIndex)
                {
                    //ignore some bookings, no need to get detail for them
                    if (booking.Value.StatusName == "Cancelled") { continue; }
                    if (booking.Value.StatusName == "Void") { continue; }

                    // ignore bookings in the past. Note we haven't fetched the detail info, so using the parent Booking model to check the date
                    string dateString = booking.Value.DateDescription;
                    DateTime parsedDate = DateTime.MinValue;
                    if (DateTime.TryParseExact(dateString, "ddd MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                    {
                        if (parsedDate >= DateTime.Now)
                        {
                              futureValidBookings.Add(booking.Value);
                        }
                    }

                }

                foreach (var booking in futureValidBookings)
                {  
                   
                    string detailUrl = apiUrl + "/" + booking.BookingId.ToString();

                    // this will get the detail for a single booking.
                    string jsonDetail = await service.GetJsonCheckFrontApiAsync(detailUrl, token).ConfigureAwait(false);

                    RootObject? detailModel = new RootObject();

                    try
                    {
                        detailModel = JsonSerializer.Deserialize<RootObject>(jsonDetail);

                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine("------------------------------------------------------");
                        // Log the exception and the problematic item
                        Debug.WriteLine($"Error deserializing item for booking Id: {booking.BookingId}");
                        Debug.WriteLine($"Exception Message: {ex.Message}");
                        // Optionally, log additional details from the exception
                        Debug.WriteLine($"{ex.Data}");
                        Debug.WriteLine($"{ex.InnerException}");
                        Debug.WriteLine("------------------------------------------------------");

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

                            foreach (KeyValuePair<string, Item> detailItem in detailModel.BookingDetail.Items)
                            {
                                booking.Studio = detailItem.Value.Studio;
                                booking.EndDate = detailItem.Value.EndDate;
                                booking.StartDate = detailItem.Value.StartDate;

                                if (detailItem.Value.EndDate > finalEndDate)
                                {
                                    finalEndDate = detailItem.Value.EndDate;
                                }
                                itemNo++;
                            }

                            booking.EndDate = finalEndDate;
                            Debug.WriteLine($"BookingId {booking.BookingId}   Detail Items count : {itemNo}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"BookingId {booking.BookingId}    itemNo: {itemNo}");
                            Debug.WriteLine($"{ex.Message}");
                        }
                    }
                     
                    // booking SHOULD be the booking model that has StartDate, EndDate, Studio
                    Debug.WriteLine("---");
                }
                listAppointments = ConvertModelToApptData(futureValidBookings, displayLocation);
            }
            return listAppointments;
        }

        /// <summary>
        /// ConvertModelToApptData
        /// </summary>
        /// <param name="modelData"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        private static List<SchedulerAppointmentData> ConvertModelToApptData(List<Booking> modelData, string displayLocation)
        {

            Debug.WriteLine("ConvertModelToApptData");

            List<SchedulerAppointmentData> appointmentData = [];

            Debug.WriteLine("ConvertModelToApptData");


            try
            {
                int id = 1;
                foreach (Booking booking in modelData)
                {

                    if (booking.BookingId == 212)
                    {
                           Debug.WriteLine("STOP");
                    }

                    //ignore some bookings, no need to get detail for them
                    if (booking.StatusName == "Cancelled")   { continue; }
                    if (booking.StatusName == "Void")        { continue; }

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

                    // Convert the string to an integer
                    //long unixTime       = booking.StartDate;

                    //var startDateTime   = ConvertTimeStamp(booking.StartDate);
                    //var endDateTime     = ConvertTimeStamp(booking.EndDate);

                    var startDateTime = UnixTimeStampToDateTime(booking.StartDate);
                    var endDateTime   = UnixTimeStampToDateTime(booking.EndDate);

                    string location = "Reserved";
                    string css = "unknown";
                    if (booking.Studio == 13)
                    {
                        // Nest
                        css = "nest"; // #1861ac;  e-appointment.
                        location = "Nest";
                    }
                    else if (booking.Studio == 14)
                    {
                        css = "hideout";    // e-appointment.
                        location = "Hideout";
                    }

                    string todStart = startDateTime.ToShortTimeString();
                    string todEnd = endDateTime.ToShortTimeString();

                    //string subject = $"{location} {todStart} - {todEnd}";
                    string subject = $"-{todEnd}"; // this is a hack to get the scheduler to display the end time in the subject field

                    string lcDisplayLocation = displayLocation.ToLower();
                    string lcLocation = location.ToLower();


                    if ((lcDisplayLocation == "nest") && (lcLocation == "nest"))
                    {
                        // add a SyncFusion SchedulerAppointmentData object to the collection
                        appointmentData.Add(new SchedulerAppointmentData
                        {
                            Id = id,
                            Subject     = subject,
                            Location    = location,
                            StartTime   = startDateTime,
                            EndTime     = endDateTime,
                            CssClass    = css
                        });
                    }
                    else if ((lcDisplayLocation == "hideout") && (lcLocation == "hideout"))
                    {
                        // add a SyncFusion AppointmentData object to the collection
                        appointmentData.Add(new SchedulerAppointmentData
                        {
                            Id          = id,
                            Subject     = subject,
                            Location    = location,
                            StartTime   = startDateTime,
                            EndTime     = endDateTime,
                            CssClass    = css
                        });
                    }
                    else
                    {
                        // add a SyncFusion AppointmentData object to the collection
                        appointmentData.Add(new SchedulerAppointmentData
                        {
                            Id          = id,
                            Subject     = subject,
                            Location    = location,
                            StartTime   = startDateTime,
                            EndTime     = endDateTime,
                            CssClass    = css
                        });
                    }

                    id++;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("ConvertModelToApptData  ---  ERROR ---------");

            }

            return appointmentData;
        }



        /// <summary>
        /// ConvertTimeStamp
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        //public static DateTime ConvertTimeStamp(long timestamp)
        //{
        //    long unixTime = timestamp;


        //    var dt = UnixTimeStampToDateTime(timestamp);



        //    // Unix epoch start
        //    DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        //    //    DateTime timestampTest = DateTime.UnixEpoch.AddSeconds(epoch.);
        //    var timestampTest = TimeProvider.System.GetUtcNow().ToUnixTimeSeconds();

        //    // 1706674064

        //    // Convert Unix timestamp to DateTime
        //    DateTimeOffset dateTimeOffset = epoch.AddSeconds(unixTime);
        //    // Print the DateTime in a readable format
        //    Console.WriteLine("ConvertTimeStamp:  Readable Date: " + dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss"));

        //    //DateTime localDateTime = dateTimeOffset.LocalDateTime;

        //    TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        //    // DateTime originalDateTime = ... // Get the original datetime
        //    DateTime localDateTime = TimeZoneInfo.ConvertTime(dateTimeOffset.DateTime, targetTimeZone);

        //    //DateTime localDateTime = dateTimeOffset.DateTime;
        //    return localDateTime;
        //}




        //public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        //{
        //    // Unix timestamp is seconds past epoch
        //    DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        //    dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        //    return dtDateTime;
        //}



        //public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        //{
        //    // Convert Unix timestamp to DateTime in UTC
        //    DateTime utcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp);

        //    // Convert UTC DateTime to PST
        //    TimeZoneInfo pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        //    DateTime pstDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, pstZone);

        //    return pstDateTime;
        //}


        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Convert Unix timestamp to DateTime in UTC
            DateTime utcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp);

            // Convert UTC DateTime to Pacific Time
            TimeZoneInfo pacificZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime pacificDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, pacificZone);

            // Check if Pacific Time is currently observing Daylight Saving Time
            bool isDaylightSavingTime = pacificZone.IsDaylightSavingTime(pacificDateTime);
            if (isDaylightSavingTime)
            {
                pacificDateTime = pacificDateTime.AddHours(-1);
            }

            return pacificDateTime;
        }






    /// <summary>
    /// SaveAppointmentsAzure
    /// </summary>
    /// <param name="listAppointments"></param>
    /// <returns></returns>

    static public async Task SaveAppointmentsAzure(List<SchedulerAppointmentData> listAppointments)
        {
            await _blobContainerClient.CreateIfNotExistsAsync();

            string jsonString = JsonSerializer.Serialize<List<SchedulerAppointmentData>>(listAppointments);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

            var blobClient = _blobContainerClient.GetBlobClient(_blobName);


            //await foreach (BlobItem blobItem in _blobContainerClient.GetBlobsAsync())
            //{
            //    Debug.WriteLine("\t" + blobItem.Name);
            //}


            // Check if the blob exists
            if (!await blobClient.ExistsAsync())
            {

                // Blob does not exist, handle the situation accordingly
                Console.WriteLine("Blob does not exist.");
                await blobClient.UploadAsync(new MemoryStream());  // an empty stream will create the blob
            }

            try
            {
                await blobClient.UploadAsync(stream, true);  // this will replace any existing data in the blob
            }
            catch (CredentialUnavailableException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// LoadAppointmentsAzure
        /// </summary>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>

        static public async Task<List<SchedulerAppointmentData>> LoadAppointmentsAzure(string displayLocation)
        {
            //  InitBlobServiceClient();


            //throw new NotImplementedException();

            string data = string.Empty; 

            var blobClient = _blobContainerClient.GetBlobClient(_blobName);

            string blobContents = string.Empty;
            await foreach (BlobItem blobItem in _blobContainerClient.GetBlobsAsync())
            {
                Debug.WriteLine("\t" + blobItem.Name);
            }

            if (!await blobClient.ExistsAsync())
            {

                // Blob does not exist, handle the situation accordingly
                Console.WriteLine("Blob does not exist.");
                throw new NotImplementedException();
            }
            try
            {
                BlobDownloadResult result = await blobClient.DownloadContentAsync();  //  .UploadAsync(stream, true);  // this will replace any existing data in the blob
                blobContents = result.Content.ToString();
            }
            catch (CredentialUnavailableException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            try
            {
                if (blobContents != null)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<SchedulerAppointmentData>();
        }

        /// <summary>
        /// FilterByDisplayLocation
        /// </summary>
        /// <param name="list"></param>
        /// <param name="displayLocation"></param>
        /// <returns></returns>
        static private List<SchedulerAppointmentData> FilterByDisplayLocation(List<SchedulerAppointmentData> list, string displayLocation)
        {
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
