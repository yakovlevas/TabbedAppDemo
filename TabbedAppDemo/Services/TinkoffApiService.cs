using System.Text.Json;
using System.Text.Json.Serialization;

namespace TabbedAppDemo.Services
{
    public class TinkoffApiService : ITinkoffApiService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey;
        private string _currentAccountId;
        private bool _isConnected;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public TinkoffApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api-invest.tinkoff.ru/openapi/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<bool> ConnectAsync(string apiKey)
        {
            try
            {
                _apiKey = apiKey?.Trim();
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new ArgumentException("API ключ не может быть пустым");
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // 1. Проверяем, что ключ валидный - запрашиваем счета
                var accountsResponse = await _httpClient.GetAsync("user/accounts");

                if (!accountsResponse.IsSuccessStatusCode)
                {
                    var errorContent = await accountsResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка API: {accountsResponse.StatusCode}. {errorContent}");
                }

                var accountsJson = await accountsResponse.Content.ReadAsStringAsync();
                var accounts = ParseAccountsResponse(accountsJson);

                if (accounts == null || accounts.Count == 0)
                {
                    throw new Exception("Не найдено брокерских счетов");
                }

                // 2. Используем первый счет по умолчанию
                _currentAccountId = accounts[0].BrokerAccountId;
                _isConnected = true;

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Ошибка сети: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Ошибка парсинга ответа API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подключения: {ex.Message}", ex);
            }
        }

        public async Task<AccountInfo> GetAccountInfoAsync()
        {
            EnsureConnected();

            try
            {
                // 1. Получаем список счетов
                var accountsResponse = await _httpClient.GetAsync("user/accounts");
                accountsResponse.EnsureSuccessStatusCode();

                var accountsJson = await accountsResponse.Content.ReadAsStringAsync();
                var accounts = ParseAccountsResponse(accountsJson);

                if (accounts == null || accounts.Count == 0)
                    throw new Exception("Счета не найдены");

                // 2. Получаем портфель для основного счета
                var portfolio = await GetPortfolioAsync();

                // 3. Формируем информацию о счете
                var mainAccount = accounts.FirstOrDefault(a => a.BrokerAccountId == _currentAccountId) ?? accounts[0];

                return new AccountInfo
                {
                    BrokerAccountId = mainAccount.BrokerAccountId,
                    BrokerAccountType = mainAccount.BrokerAccountType,
                    Status = "Active",
                    OpenedDate = DateTime.Now.AddYears(-1), // В реальном API эта информация приходит отдельно
                    LastUpdate = DateTime.Now,
                    TotalBalance = portfolio.TotalPortfolioValue,
                    ExpectedYield = portfolio.ExpectedYield,
                    Currency = "RUB",
                    TotalAccounts = accounts.Count
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения информации о счете: {ex.Message}", ex);
            }
        }

        public async Task<PortfolioInfo> GetPortfolioAsync()
        {
            EnsureConnected();

            try
            {
                var url = string.IsNullOrEmpty(_currentAccountId)
                    ? "portfolio"
                    : $"portfolio?brokerAccountId={_currentAccountId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return ParsePortfolioResponse(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения портфеля: {ex.Message}", ex);
            }
        }

        public async Task<List<Account>> GetAccountsAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync("user/accounts");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return ParseAccountsResponse(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения списка счетов: {ex.Message}", ex);
            }
        }

        public Task<bool> IsConnected() => Task.FromResult(_isConnected);

        public void Disconnect()
        {
            _isConnected = false;
            _apiKey = null;
            _currentAccountId = null;
            _httpClient.DefaultRequestHeaders.Clear();
        }

        #region Private Methods

        private void EnsureConnected()
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Не подключено к Tinkoff API. Сначала выполните ConnectAsync.");
        }

        private List<Account> ParseAccountsResponse(string json)
        {
            try
            {
                var response = JsonSerializer.Deserialize<TinkoffApiResponse<AccountsPayload>>(json, _jsonOptions);

                return response?.Payload?.Accounts?.Select(a => new Account
                {
                    BrokerAccountType = a.BrokerAccountType,
                    BrokerAccountId = a.BrokerAccountId
                }).ToList() ?? new List<Account>();
            }
            catch (JsonException)
            {
                // Альтернативный парсинг для другой структуры ответа
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("payload", out var payload) &&
                        payload.TryGetProperty("accounts", out var accounts))
                    {
                        var result = new List<Account>();
                        foreach (var account in accounts.EnumerateArray())
                        {
                            result.Add(new Account
                            {
                                BrokerAccountType = account.GetProperty("brokerAccountType").GetString(),
                                BrokerAccountId = account.GetProperty("brokerAccountId").GetString()
                            });
                        }
                        return result;
                    }
                }
                catch
                {
                    // Если и этот парсинг не удался
                }

                return new List<Account>();
            }
        }

        private PortfolioInfo ParsePortfolioResponse(string json)
        {
            var portfolioInfo = new PortfolioInfo
            {
                Positions = new List<PortfolioPosition>(),
                Currency = "RUB"
            };

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("payload", out var payload))
                {
                    // Парсим позиции
                    if (payload.TryGetProperty("positions", out var positions))
                    {
                        foreach (var position in positions.EnumerateArray())
                        {
                            var portfolioPosition = new PortfolioPosition();

                            if (position.TryGetProperty("figi", out var figi))
                                portfolioPosition.Figi = figi.GetString();

                            if (position.TryGetProperty("ticker", out var ticker))
                                portfolioPosition.Ticker = ticker.GetString();

                            if (position.TryGetProperty("name", out var name))
                                portfolioPosition.Name = name.GetString();

                            if (position.TryGetProperty("instrumentType", out var instrumentType))
                                portfolioPosition.InstrumentType = instrumentType.GetString();

                            if (position.TryGetProperty("balance", out var balance))
                                portfolioPosition.Balance = balance.GetDecimal();

                            if (position.TryGetProperty("averagePositionPrice", out var avgPrice) &&
                                avgPrice.TryGetProperty("value", out var avgPriceValue))
                                portfolioPosition.AveragePositionPrice = avgPriceValue.GetDecimal();

                            if (position.TryGetProperty("expectedYield", out var yield) &&
                                yield.TryGetProperty("value", out var yieldValue))
                                portfolioPosition.ExpectedYield = yieldValue.GetDecimal();

                            // Для текущей цены нужно делать отдельный запрос к market/orderbook
                            // Здесь используем среднюю цену как текущую для демонстрации
                            portfolioPosition.CurrentPrice = portfolioPosition.AveragePositionPrice;

                            portfolioInfo.Positions.Add(portfolioPosition);
                        }
                    }

                    // Рассчитываем общую стоимость портфеля
                    portfolioInfo.TotalPortfolioValue = portfolioInfo.Positions
                        .Sum(p => p.Balance * p.CurrentPrice);

                    // Получаем ожидаемую доходность
                    if (payload.TryGetProperty("expectedYield", out var totalYield) &&
                        totalYield.TryGetProperty("value", out var totalYieldValue))
                    {
                        portfolioInfo.ExpectedYield = totalYieldValue.GetDecimal();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга портфеля: {ex.Message}");
                // Возвращаем пустой портфель с базовыми значениями
                portfolioInfo.TotalPortfolioValue = 0;
                portfolioInfo.ExpectedYield = 0;
            }

            return portfolioInfo;
        }

        #endregion

        #region Response Models

        private class TinkoffApiResponse<T>
        {
            [JsonPropertyName("trackingId")]
            public string TrackingId { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("payload")]
            public T Payload { get; set; }
        }

        private class AccountsPayload
        {
            [JsonPropertyName("accounts")]
            public List<TinkoffAccount> Accounts { get; set; }
        }

        private class TinkoffAccount
        {
            [JsonPropertyName("brokerAccountType")]
            public string BrokerAccountType { get; set; }

            [JsonPropertyName("brokerAccountId")]
            public string BrokerAccountId { get; set; }
        }

        #endregion
    }
}