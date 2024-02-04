using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CheckFrontAzureFunction
{
    public class TimerTriggerFunction
    {
        private readonly ILogger _logger;

        public TimerTriggerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimerTriggerFunction>();
        }

        /// <summary>
        /// TimerTrigger("0 0 * * * *") is once each hour
        /// TimerTrigger("0 0 */6 * * *") is 4x daily
        /// </summary>
        /// <param name="myTimer"></param>
        [Function("TimerTriggerFunction")]
        public void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
