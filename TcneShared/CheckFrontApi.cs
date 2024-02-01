using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace TcneShared
{
    public class CheckFrontApiService
    {
        private readonly HttpClient _httpClient;
        private IConfiguration? _configuration;


        public CheckFrontApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public string GetBasicAuthToken()
        {
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
            if (_configuration == null)
            {
                throw new ArgumentNullException(nameof(_configuration));
            }
            string? pingUrl = _configuration["CheckFront_Api_Ping_Url"];

            if (!String.IsNullOrEmpty(pingUrl))
            {
                //   GET /api/3.0/ping
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

            // Log the request details
            Debug.WriteLine($"HTTP GET Request to: {apiUrl}");
            Debug.WriteLine($"Authorization Header: Basic {token}");


            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

            var response = await _httpClient.GetAsync(apiUrl).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            // Log the response details
            Debug.WriteLine($"HTTP Response Status Code: {response.StatusCode}");
            Debug.WriteLine($"HTTP Response Content: {content}");


            return content;
        }

    }
}
