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

            _logger.LogInformation($"Tcne HTTP Function activated :  {DateTime.Now}");

            try
            {
                if ((req is not null) && (req.Body is not null))
                {
                    // This is likely to be a CheckFront WebHook request, else it's a test request via the browser or Postman
                    // convert to string and log
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(requestBody))
                    {
                        _logger.LogError("Request body string is empty");                        
                    }
                    else
                    {
                        _logger.LogInformation($"Raw Body: {requestBody}");
                    }   

                    // We don't need to deserialize the request body, this is just a notification to call the CheckFront API and update the Azure Storage

                    // Deserialize the original request body stream into TcneShared.WebHook.Root
                    //TcneShared.WebHook.Root? root = JsonSerializer.Deserialize<TcneShared.WebHook.Root>(requestBody);
                }


            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: CheckFrontFunction {ex.Message}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("TCNE function activated");

            await UpdateStorageFromCheckFront();

            return response;
        }

        private async Task UpdateStorageFromCheckFront()
        {
            if (_logger is null)
            {
                throw new ArgumentNullException(nameof(_logger));
            }
            if (_checkFrontApiService is null)
            {
                throw new ArgumentNullException(nameof(_checkFrontApiService));
            }
            if (_azureStorageService is null)
            {
                throw new ArgumentNullException(nameof(_azureStorageService));
            }

            _logger.LogInformation("UpdateStorageFromCheckFront");

            string token = _checkFrontApiService.GetBasicAuthToken();
            _logger.LogInformation($"Token: {token}");

            bool apiAvailable = await _checkFrontApiService.PingCheckFrontApi();
            _logger.LogInformation($"CheckFront API available: {apiAvailable}");

            if (!apiAvailable)
            {
                // TODO: Add code to signal an alert for CheckFront service unavailability
                // await SendAlert();
                _logger.LogError($"PingCheckFrontApi failed : CheckFront API NOT AVAILABLE: {apiAvailable}");
                return;
            }
            List<SchedulerAppointmentData> listAppointments = new();

            listAppointments = await _azureStorageService.GetAppointments();        // fetch from CheckFront API

            Debug.WriteLine($"listAppointments.Count: {listAppointments.Count}");

            await _azureStorageService.SaveAppointmentsAzure(listAppointments);     // save to Azure Storage
        }

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
