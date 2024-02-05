using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TcneShared
{
    public class CheckFrontApiService
    {
        private readonly HttpClient     _httpClient;
        private IConfiguration?         _configuration;
        ILogger<CheckFrontApiService>   _logger;

        public CheckFrontApiService(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration, 
            ILogger<CheckFrontApiService> logger)
        {
            _httpClient     = httpClientFactory.CreateClient();
            _configuration  = configuration;
            _logger         = logger;
            _logger.LogInformation("CheckFrontApiService Constructor");
        }

        public string GetBasicAuthToken()
        {
            _logger.LogInformation("GetBasicAuthToken");

            if (_configuration == null)
            {
                throw new ArgumentNullException(nameof(_configuration));
            }
            string? username = _configuration["HTTP BASIC Username"];
            string? password = _configuration["HTTP BASIC Password"];

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            return Convert.ToBase64String(byteArray);
        }

        public async Task<bool> PingCheckFrontApi()
        {
            _logger.LogInformation("PingCheckFrontApi");

            if (_configuration == null)
            {
                throw new ArgumentNullException(nameof(_configuration));
            }
            string? pingUrl = _configuration["CheckFront_Api_Ping_Url"];

            if (!String.IsNullOrEmpty(pingUrl))
            {
                //   GET /api/3.0/ping
                // using ConfigureAwait means don't need to resume execution on the original context
                var responsePing = await _httpClient.GetAsync(pingUrl).ConfigureAwait(false);
                if (responsePing.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<string> GetJsonCheckFrontApiAsync(string apiUrl, string token)
        {
            _logger.LogInformation("GetJsonCheckFrontApiAsync");

            // Log the request details
            Debug.WriteLine($"HTTP GET Request to: {apiUrl}");
            Debug.WriteLine($"Authorization Header: Basic {token}");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

            try
            {
                // using ConfigureAwait means don't need to resume execution on the original context
                var response = await _httpClient.GetAsync(apiUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                // Log the response details
                Debug.WriteLine($"HTTP Response Status Code: {response.StatusCode}");
                Debug.WriteLine($"HTTP Response Content: {content}");

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetJsonCheckFrontApiAsync:  error getting json from CheckFront API");
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }
}
