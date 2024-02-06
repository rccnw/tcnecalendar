using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TcneShared;
using TcneShared.Models;
using TcneShared.WebHook;
using System.Text.Json;
using Grpc.Core;
using Azure;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckFrontAzureFunction
{
    public class CheckFrontFunction
    {
        private readonly ILogger? _logger;
        private readonly CheckFrontApiService? _checkFrontApiService;
        private readonly AzureStorage? _azureStorageService;
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        public CheckFrontFunction(ILoggerFactory loggerFactory, AzureStorage azureStorageService, CheckFrontApiService checkFrontApiService)
        {
            _logger = loggerFactory.CreateLogger<CheckFrontFunction>();
            _azureStorageService = azureStorageService;
            _checkFrontApiService = checkFrontApiService;
        }

        [Function("CheckFrontFunction")]
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

            _logger.LogInformation($"Tcne HTTP Function activated :  {DateTime.Now}");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("CheckFrontFunction function activated");      // gratuituous response

            try
            {
                if (_azureStorageService is not null)
                {
                    if (await Semaphore.WaitAsync(0))  // Try to acquire the semaphore.
                    {
                        try
                        {
                            await _azureStorageService.UpdateStorageFromCheckFront();
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
                throw;
            }

            return response;
        }
    }
}
