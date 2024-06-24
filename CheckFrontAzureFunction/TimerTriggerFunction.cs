using System;
using System.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TcneShared;


namespace CheckFrontAzureFunction
{

    public class TimerTriggerFunction
    {
        private readonly ILogger _logger;
        private readonly CheckFrontApiService? _checkFrontApiService;
        private readonly AzureStorage? _azureStorageService;

        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        public TimerTriggerFunction(ILoggerFactory loggerFactory, AzureStorage azureStorageService, CheckFrontApiService checkFrontApiService)
        {
            _logger = loggerFactory.CreateLogger<TimerTriggerFunction>();
            _azureStorageService    = azureStorageService;
            _checkFrontApiService   = checkFrontApiService;
        }

        /// <summary>
        /// TimerTrigger("0 0 * * * *") is once each hour
        /// TimerTrigger("0 0 */6 * * *") is 4x daily
        /// </summary>
        /// <param name="myTimer"></param>
        [Function("TimerTriggerFunction")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
            try
            {
                if (_azureStorageService is not null)
                {
                    var lastRunTime = await _azureStorageService.GetLastWebhookRunTime();

                    if (lastRunTime is not null && DateTimeOffset.Now - lastRunTime.Value >= TimeSpan.FromMinutes(30))
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
                            _logger.LogInformation("TimerTriggerFunction:  Previous call to UpdateStorageFromCheckFront is still running");
                        }

                    }
                    else { _logger.LogInformation("TimerTriggerFunction:  Not enough time has passed since last run"); }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TimerTriggerFunction:   UpdateStorageFromCheckFront failed - {ex.Message}");
                //throw;
            }
        }
    }
}
