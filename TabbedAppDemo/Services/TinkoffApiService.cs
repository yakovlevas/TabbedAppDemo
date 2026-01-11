using System.Text.Json;

namespace TabbedAppDemo.Services
{
    public class TinkoffApiService : ITinkoffApiService
    {
        private readonly HttpClient _httpClient;
        private bool _isConnected = false;

        public TinkoffApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api-invest.tinkoff.ru/openapi/")
            };
        }

        public async Task<bool> ConnectAsync(string apiKey)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.GetAsync("user/accounts");
                _isConnected = response.IsSuccessStatusCode;
                return _isConnected;
            }
            catch
            {
                _isConnected = false;
                return false;
            }
        }

        public async Task<AccountInfo> GetAccountInfoAsync()
        {
            // Возвращаем тестовые данные
            return new AccountInfo
            {
                BrokerAccountId = "T" + DateTime.Now.Ticks.ToString()[^10..],
                Status = "Active",
                LastUpdate = DateTime.Now,
                TotalBalance = 150000.75m,
                TotalAccounts = 1
            };
        }

        public Task<bool> IsConnected() => Task.FromResult(_isConnected);
    }
}