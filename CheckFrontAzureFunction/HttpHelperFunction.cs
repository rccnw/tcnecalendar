using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TcneShared;

namespace CheckFrontAzureFunction
{
    public class HttpHelperFunction
    {
        private readonly CheckFrontApiService?  _checkFrontApiService;
        private readonly AzureStorage?          _azureStorageService;
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger<HttpHelperFunction> _logger;

        public HttpHelperFunction(ILoggerFactory loggerFactory, AzureStorage azureStorageService, CheckFrontApiService checkFrontApiService)
        {
            _logger = loggerFactory.CreateLogger<HttpHelperFunction>();
            _azureStorageService = azureStorageService;
            _checkFrontApiService = checkFrontApiService;
        }


        // This function is activated by the CheckFrontFunction as a means to offload the long running task of updating the storage

        [Function("HttpHelperFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            if (_logger is null)
            {
                throw new ArgumentNullException(nameof(_logger));
            }
            if (_azureStorageService is null)
            {
                throw new ArgumentNullException(nameof(_azureStorageService));
            }
            // Start the stopwatch to measure the execution time
            Stopwatch stopwatch = Stopwatch.StartNew();
            int apiCallCount = 0;

            _logger.LogInformation($"HttpHelperFunction HTTP Function activated :  {DateTime.Now}");

            // the response to CheckFrontFunction is gratuitous, it didn't wait 
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("HttpHelperFunction function activated");      

            try
            {
                if (_azureStorageService is not null)
                {
                    if (await Semaphore.WaitAsync(0))  // Try to acquire the semaphore.
                    {
                        try
                        {
                            apiCallCount = await _azureStorageService.UpdateStorageFromCheckFront();
                            await _azureStorageService.SetWebhookRunTime();
                        }
                        finally
                        {
                            Semaphore.Release();  // Release the semaphore.
                        }
                    }
                    else
                    {
                        _logger.LogInformation("CheckFrontFunction:  Previous call to UpdateStorageFromCheckFront is still running");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CheckFrontFunction {ex.Message}");
            }
            // Stop the stopwatch
            stopwatch.Stop();

            // Report
            _logger.LogInformation($"HttpHelperFunction executed in {stopwatch.ElapsedMilliseconds} milliseconds");
            _logger.LogInformation($"HttpHelperFunction executed {apiCallCount} API calls");


            var callsPerMinute = ((apiCallCount / (stopwatch.ElapsedMilliseconds / 1000)) * 60);
       
            _logger.LogInformation($"HttpHelperFunction executed at the equivalent rate of {callsPerMinute} API calls per minute");

            return response;
        }
    }
}
