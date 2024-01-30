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

        public static async Task<List<SchedulerAppointmentData>> UpdateStorageAccount(HttpClient httpClient, IConfiguration Configuration, string displayLocation)
        {
            List<SchedulerAppointmentData> listAppointments = new List<SchedulerAppointmentData>();

            if (_updateStorageAccount)
            {
                listAppointments = await GetAppointments(httpClient, Configuration, displayLocation);
                Debug.WriteLine($"listAppointments.Count: {listAppointments.Count}");
                //await SaveAppointmentsAzure(listAppointments);
            }
            // in any case, fetch the list of appointments from Azure Storage
            listAppointments = await AzureStorage.LoadAppointmentsAzure();
            return listAppointments;
        }



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

            // now we have the list of bookings  from CheckFront, we need to get the detail for each booking and merge it into the list



            if (bookingModel != null)
            {
                foreach (KeyValuePair<string, Booking> booking in bookingModel.BookingIndex)
                {
                    //ignore some bookings, no need to get detail for them
                    if (booking.Value.StatusName == "Cancelled") { continue; }
                    if (booking.Value.StatusName == "Void")      { continue; }


                    //if (booking.Value.BookingId == 170)
                    //{
                    //    Debug.WriteLine("204");
                    //}
                    //else
                    //{
                    //    continue;
                    //}

                    Debug.WriteLine($"Key: {booking.Key}, Value: {booking.Value.StatusName}");



                    //var bookId = booking.BookingId


                    {
                        string detailUrl = apiUrl + "/" + booking.Value.BookingId.ToString();

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
                            Debug.WriteLine($"Error deserializing item for booking Id: {booking.Value.BookingId}");
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


                            // using the date_desc string field in the parent booking data, get the booking date (but not the time)
                            // e.g. :       "date_desc": "Thu Jun 29, 2023",



                            // TODO set booking time for 8am-11pm, lacking any other indication of actual time
                            // TODO need to creat a long for each 
                            //booking.Value.StartDate = date;
                            //booking.Value.EndDate   = date;




                            string dateString = booking.Value.DateDescription;

                            DateTime parsedDate;

                            if (DateTime.TryParseExact(dateString, "ddd MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                            {
                                // Successfully parsed
                                // booking.Value.StartDate

                                // Assuming parsedDate is in UTC. If not, convert it to UTC.
                                long unixTimeStart = new DateTimeOffset(parsedDate).ToUnixTimeSeconds(); // or ToUnixTimeMilliseconds for milliseconds

                                booking.Value.StartDate = unixTimeStart;
                                booking.Value.EndDate = unixTimeStart;
                                booking.Value.Studio = Int32.Parse(Configuration["StudioCategoryUnknown"]);
                            }


                            else
                            {
                                // Handle parsing error

                                Debug.WriteLine("GetScheduleDataCheckFront parsing error.  Booking detail json can't be deserialized. ");
                            }
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
                                    booking.Value.Studio = detailItem.Value.Studio;
                                    booking.Value.EndDate = detailItem.Value.EndDate;
                                    booking.Value.StartDate = detailItem.Value.StartDate;

                                    if (detailItem.Value.EndDate > finalEndDate)
                                    {
                                        finalEndDate = detailItem.Value.EndDate;
                                    }
                                    itemNo++;
                                }

                                booking.Value.EndDate = finalEndDate;
                                Debug.WriteLine($"BookingId {booking.Value.BookingId}   Detail Items count : {itemNo}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"BookingId {booking.Value.BookingId}    itemNo: {itemNo}");
                                Debug.WriteLine($"{ex.Message}");
                            }


                        }

                        Debug.WriteLine("---");

                    }
                    // booking SHOULD be the booking model that has StartDate, EndDate, Studio
                    Debug.WriteLine("---");
                }

                listAppointments = ConvertModelToApptData(bookingModel, displayLocation);
            }

            //var bookId = booking.BookingId

            //string detailUrl = apiUrl + "//" + booking.BookingId.ToString();
            //string jsonDetail = await service.GetJsonCheckFrontApiAsync(detailUrl, token).ConfigureAwait(false);

            Debug.WriteLine("--------------------------------------------");


            //LoadCalendarData loadCalendarData = new LoadCalendarData();

            //Root bookingModelData = loadCalendarData.GetProjectJsonBookingData().Result; // This does not get the start/end time info from booking_id API

            //CheckFrontBookingDetail.RootObject modelDataIds = await loadCalendarData.GetProjectJsonBookingDetailData();

            //var models = new ConvertModels(_configuration);    doesn't do anything yet except capture config info



            return listAppointments;
        }






        private static List<SchedulerAppointmentData> ConvertModelToApptData(Root modelData, string displayLocation)
        {

            Debug.WriteLine("ConvertModelToApptData");

            List<SchedulerAppointmentData> appointmentData = new();

            Debug.WriteLine("ConvertModelToApptData");
            Debug.WriteLine("===================================");
            Debug.WriteLine($"HostId              : {modelData.HostId}");
            Debug.WriteLine($"Name                : {modelData.Name}");
            Debug.WriteLine($"Records             : {modelData.Request.Records}");
            Debug.WriteLine($"TotalRecords        : {modelData.Request.TotalRecords}");
            Debug.WriteLine("===================================");

            try
            {
                int id = 1;
                foreach (var item in modelData.BookingIndex)
                {
                    //ignore some bookings, no need to get detail for them
                    if (item.Value.StatusName == "Cancelled")   { continue; }
                    if (item.Value.StatusName == "Void")        { continue; }

                    Debug.WriteLine($"StatusName:  {item.Value.StatusName}");

                    // Access the key and value of each item in the dictionary
                    var key = item.Key;
                    var booking = item.Value;


                    Debug.WriteLine($"BookingId       : {booking.BookingId}");
                    Debug.WriteLine($"Summary         : {booking.Summary}");
                    Debug.WriteLine($"StatusId        : {booking.StatusId}");
                    Debug.WriteLine($"StatusName      : {booking.StatusName}");
                    Debug.WriteLine($"CustomerName    : {booking.CustomerName}");
                    Debug.WriteLine($"DateDescription : {booking.DateDescription}");
                    Debug.WriteLine("===================================");

                    // A string describing the booked dates/times
                    // booking.DateDescription   


                    //"start_date": 1703894400,
                    //"end_date": 1703901600,

                    //  var start_date = ConvertTimeStamp(1703894400);
                    //  var end_date   = ConvertTimeStamp(1703901600);


                    // Convert the string to an integer
                    long unixTime = booking.StartDate;

                    var startDateTime = ConvertTimeStamp(booking.StartDate);
                    var endDateTime = ConvertTimeStamp(booking.EndDate);


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
                    string subject = $"{location} {todStart} - {todEnd}";


                    if ((displayLocation == "nest") && (location == "Nest"))
                    {
                        // add a SyncFusion AppointmentData object to the collection
                        appointmentData.Add(new SchedulerAppointmentData
                        {
                            Id = id,
                            Subject = subject,
                            Location = location,
                            StartTime = startDateTime,
                            EndTime = endDateTime,
                            CssClass = css
                        });
                    }
                    else if ((displayLocation == "hideout") && (location == "Hideout"))
                    {
                        // add a SyncFusion AppointmentData object to the collection
                        appointmentData.Add(new SchedulerAppointmentData
                        {
                            Id = id,
                            Subject = subject,
                            Location = location,
                            StartTime = startDateTime,
                            EndTime = endDateTime,
                            CssClass = css
                        });
                    }
                    else
                    {
                        // add a SyncFusion AppointmentData object to the collection
                        appointmentData.Add(new SchedulerAppointmentData
                        {
                            Id = id,
                            Subject = subject,
                            Location = location,
                            StartTime = startDateTime,
                            EndTime = endDateTime,
                            CssClass = css
                        });
                    }



                    id++;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("ConvertModelToApptData  ---  ERROR ---------");
                //  throw;
            }

            // throw new NotImplementedException();
            return appointmentData;
        }

        public static DateTime ConvertTimeStamp(long timestamp)
        {
            long unixTime = timestamp;
            // Unix epoch start
            DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            // Convert Unix timestamp to DateTime
            DateTimeOffset dateTimeOffset = epoch.AddSeconds(unixTime);
            // Print the DateTime in a readable format
            Console.WriteLine("ConvertTimeStamp:  Readable Date: " + dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss"));

            DateTime localDateTime = dateTimeOffset.LocalDateTime;
            return localDateTime;
        }













        static public async Task SaveAppointmentsAzure(List<SchedulerAppointmentData> listAppointments)
        {
            await _blobContainerClient.CreateIfNotExistsAsync();

            string jsonString = JsonSerializer.Serialize<List<SchedulerAppointmentData>>(listAppointments);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

            var blobClient = _blobContainerClient.GetBlobClient(_blobName);


            await foreach (BlobItem blobItem in _blobContainerClient.GetBlobsAsync())
            {
                Debug.WriteLine("\t" + blobItem.Name);
            }



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


        static public async Task<List<SchedulerAppointmentData>> LoadAppointmentsAzure()
        {
            //  InitBlobServiceClient();


            //throw new NotImplementedException();

            string data = string.Empty; // TODO this needs storage data <<<<<<<<<<<<<<<<<<<<<<<<<

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
                            return list;
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
    }
}
