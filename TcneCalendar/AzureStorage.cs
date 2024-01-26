using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.Text;
using Azure.Identity;
using TcneCalendar.Data;


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


        static string _accountName = string.Empty;
        static string _containerName = string.Empty;
        static string _blobName = string.Empty;
        static string _storageKey = string.Empty;
        static string _storageConnectionString = string.Empty;

        //        static ILogger<AzureStorage> _logger;


        public static void InitBlobServiceClient(IConfiguration Configuration)  // , ILogger<AzureStorage> logger
        {
            // var credential = new DefaultAzureCredential();  this was for MSI

            if (Configuration == null)
            {
                throw new ArgumentNullException(nameof(Configuration));
            }

            _accountName = Configuration["AzureStorageAccountName"];
            _containerName = Configuration["AzureStorageAccountContainerName"];
            _blobName = Configuration["AzureStorageBlobName"];
            _storageKey = Configuration["StorageKey"];
            _storageConnectionString = Configuration["StorageConnectionString"];

            _blobServiceClient = new BlobServiceClient(_storageConnectionString);
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        }

        static public async Task SaveAppointmentsAzure(List<ScheduleData.AppointmentData> listAppointments)
        {
            await _blobContainerClient.CreateIfNotExistsAsync();

            string jsonString = JsonSerializer.Serialize<List<ScheduleData.AppointmentData>>(listAppointments);
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


        static public async Task<List<ScheduleData.AppointmentData>> LoadAppointmentsAzure()
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
                        List<ScheduleData.AppointmentData>? list = await JsonSerializer.DeserializeAsync<List<ScheduleData.AppointmentData>>(stream);
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
            return new List<ScheduleData.AppointmentData>();
        }
    }
}
