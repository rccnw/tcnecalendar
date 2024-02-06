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
            if(_azureStorageService is null)
            {
                throw new ArgumentNullException(nameof(_azureStorageService));
            }

            _logger.LogInformation($"Tcne HTTP Function activated :  {DateTime.Now}");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("TCNE function activated");

            try
            {
                //if (_azureStorageService is not null)
                //{
                //    await _azureStorageService.UpdateStorageFromCheckFront();
                //}

                await _azureStorageService.SetWebhookRunTime();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Http Function:   UpdateStorageFromCheckFront failed  {ex.Message}");
                throw;
            }

            return response;
        }


        //private async void StoreLastRunTime()
        //{

        //    // Write the current time to the table.
        //    var entity = new TableEntity("Webhook", "LastRun")
        //    {
        //        { "Timestamp", DateTime.UtcNow }
        //    };
        //    await table.ExecuteAsync(TableOperation.InsertOrReplace(entity));


        //}


        //private async Task SendAlert()
        //{
        //_logger.LogInformation("Sending alert for CheckFront service unavailability");

        //string recipient    = "admin@example.com";
        //string subject      = "CheckFront Service Unavailability";
        //string body         = "The CheckFront service is currently unavailable.";

        //string sendGridApiKey = Environment.GetEnvironmentVariable("SendGridApiKey");

        //var client = new SendGridClient(sendGridApiKey);
        //var msg = new SendGridMessage()
        //{
        //    From = new EmailAddress("sender@example.com", "Sender Name"),
        //    Subject = subject,
        //    PlainTextContent = body,
        //    HtmlContent = $"<p>{body}</p>"
        //};
        //msg.AddTo(new EmailAddress(recipient));

        //var response = await client.SendEmailAsync(msg);

        //if (response.StatusCode != HttpStatusCode.Accepted)
        //{
        //    _logger.LogError($"Failed to send email alert. Status code: {response.StatusCode}");
        //}
        //else
        //{
        //    _logger.LogInformation("Email alert sent successfully");
        //}
        //}
    }
}
