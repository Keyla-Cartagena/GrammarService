using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;


namespace GrammarService.Services
{
    public class TransformClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public TransformClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private string BaseUrl => _config["TransformService:BaseUrl"];

        public async Task<object?> FactorizeAsync(object grammar)
        {
            var url = $"{BaseUrl}/factorize";
            var response = await _httpClient.PostAsJsonAsync(url, grammar);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }

        public async Task<object?> EliminateRecursionAsync(object grammar)
        {
            var url = $"{BaseUrl}/eliminate-recursion";
            var response = await _httpClient.PostAsJsonAsync(url, grammar);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }
    }
}
