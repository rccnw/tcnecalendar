using System;
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
                    await _azureStorageService.UpdateStorageFromCheckFront();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"TimerTriggerFunction:   UpdateStorageFromCheckFront failed - {ex.Message}");
                throw;
            }
        }
    }
}
