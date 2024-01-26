using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;

namespace TcneCalendar
{
    public class CheckFrontApi
    {
    }

    public class CheckFrontApiService
    {
        string username = "";
        string password = "";

        private readonly HttpClient _httpClient;


        private IConfiguration? _configuration;


        public CheckFrontApiService(HttpClient httpClient, IConfiguration configuration)
        {
            throw new NotImplementedException();  // need DI configuration for username password

            _httpClient     = httpClient;
            _configuration  = configuration;
        }

        public string GetBasicAuthToken()  // string username, string password   TODO store in App Service Config and access from there
        {
            string username = _configuration["HTTP BASIC Username"];
            string password = _configuration["HTTP BASIC Password"];

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            return Convert.ToBase64String(byteArray);
        }

        public async Task<string> GetJsonCheckFrontApiAsync(string apiUrl, string token)
        {
            //   GET /api/3.0/ping
            //string pingUrl = @"https://tcne.checkfront.com/api/3.0/ping";
            //var responsePing = await _httpClient.GetAsync(pingUrl).ConfigureAwait(false);



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

            //return await Task.FromResult(content);
            return content;
        }
    }
}
