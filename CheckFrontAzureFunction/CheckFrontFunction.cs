using System.Net;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TcneShared;


namespace CheckFrontAzureFunction
{
    public class CheckFrontFunction
    {
        private readonly ILogger? _logger;
        private IConfiguration? _configuration;

        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private static readonly HttpClient _httpClient = new HttpClient();

        public CheckFrontFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger         = loggerFactory.CreateLogger<CheckFrontFunction>();
            _configuration  = configuration;
        }

        [Function("CheckFrontFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            if (_logger is null)
            {
                throw new ArgumentNullException(nameof(_logger));
            }
            if (_configuration is null)
            {
                throw new ArgumentNullException(nameof(_configuration));
            }

            _logger.LogInformation($"CheckFrontFunction HTTP Function activated :  {DateTime.Now}");

            try
            {
                // configure response to CheckFront webhook call
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("CheckFrontFunction function activated");      // gratuituous response

                // Make an HTTP request to the HttpHelperFunction endpoint asynchronously
                var httpHelperFunctionUrl = _configuration["HttpHelperFunctionUrl"];
                _logger.LogInformation($"CheckFrontFunction:  calling HttpHelperFunction  - '{httpHelperFunctionUrl}'");

                var httpClient = new HttpClient();

                httpClient.PostAsync(httpHelperFunctionUrl, null);      // don't wait, just fire and forget

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CheckFrontFunction:  Exception thrown  {ex.Message}");
                throw;
            }
        }
    }
}
