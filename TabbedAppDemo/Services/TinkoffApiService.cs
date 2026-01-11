using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using static TabbedAppDemo.Services.TinkoffApiService;

namespace TabbedAppDemo.Services
{
    public class TinkoffApiService : ITinkoffApiService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey;
        private string _currentAccountId;
        private bool _isConnected;
        private Dictionary<string, string> _figiToTickerCache = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            Converters = {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new BooleanConverter(),
                new StringLongConverter(),
                new QuotationConverter()
            }
        };

        private const string TOKEN_FILENAME = "tinkoff_token.dat";
        private static readonly string TokenFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "TabbedAppDemo", TOKEN_FILENAME);

        public TinkoffApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://invest-public-api.tinkoff.ru/rest/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
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

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                // Проверяем подключение - запрашиваем информацию о пользователе
                var request = new { };
                var response = await SendRequest<GetInfoResponse>(
                    "tinkoff.public.invest.api.contract.v1.UsersService/GetInfo",
                    request);

                if (response == null)
                {
                    throw new Exception("Не удалось получить информацию о пользователе");
                }

                // Получаем список счетов
                var accountsResponse = await SendRequest<GetAccountsResponse>(
                    "tinkoff.public.invest.api.contract.v1.UsersService/GetAccounts",
                    new { });

                if (accountsResponse?.Accounts == null || accountsResponse.Accounts.Count == 0)
                {
                    throw new Exception("Не найдено брокерских счетов");
                }

                // Используем первый счет по умолчанию
                _currentAccountId = accountsResponse.Accounts[0].Id;
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
                // Получаем список счетов
                var accountsResponse = await SendRequest<GetAccountsResponse>(
                    "tinkoff.public.invest.api.contract.v1.UsersService/GetAccounts",
                    new { });

                if (accountsResponse?.Accounts == null || accountsResponse.Accounts.Count == 0)
                    throw new Exception("Счета не найдены");

                // Получаем портфель для основного счета
                var portfolio = await GetPortfolioAsync();

                // Получаем информацию о пользователе
                var userInfo = await SendRequest<GetInfoResponse>(
                    "tinkoff.public.invest.api.contract.v1.UsersService/GetInfo",
                    new { });

                // Формируем информацию о счете
                var mainAccount = accountsResponse.Accounts.FirstOrDefault(a => a.Id == _currentAccountId)
                    ?? accountsResponse.Accounts[0];

                return new AccountInfo
                {
                    BrokerAccountId = mainAccount.Id,
                    BrokerAccountType = GetAccountType(mainAccount.Type),
                    Status = GetAccountStatus(mainAccount.Status),
                    OpenedDate = mainAccount.OpenedDate,
                    LastUpdate = DateTime.Now,
                    TotalBalance = portfolio.TotalPortfolioValue,
                    ExpectedYield = portfolio.ExpectedYield,
                    Currency = "RUB", // По умолчанию
                    TotalAccounts = accountsResponse.Accounts.Count
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
                var request = new
                {
                    accountId = _currentAccountId,
                    currency = "RUB"
                };

                var response = await SendRequest<GetPortfolioResponse>(
                    "tinkoff.public.invest.api.contract.v1.OperationsService/GetPortfolio",
                    request);

                if (response == null)
                    throw new Exception("Пустой ответ от API");

                var portfolioInfo = new PortfolioInfo
                {
                    Positions = new List<PortfolioPosition>(),
                    Currency = "RUB"
                };

                // Рассчитываем общую стоимость
                if (response.TotalAmountPortfolio != null)
                {
                    portfolioInfo.TotalPortfolioValue = response.TotalAmountPortfolio.ToDecimal();
                }

                // Обрабатываем позиции
                if (response.Positions != null)
                {
                    foreach (var position in response.Positions)
                    {
                        var portfolioPosition = new PortfolioPosition
                        {
                            Figi = position.Figi,
                            InstrumentType = position.InstrumentType,
                            Balance = position.Quantity?.ToDecimal() ?? 0,
                            CurrentPrice = position.CurrentPrice?.ToDecimal() ?? 0
                        };

                        // Получаем тикер для FIGI
                        portfolioPosition.Ticker = await GetTickerForFigi(position.Figi);

                        // Получаем название инструмента
                        portfolioPosition.Name = await GetInstrumentName(position.Figi);

                        // Рассчитываем среднюю цену (для упрощения используем текущую)
                        portfolioPosition.AveragePositionPrice = portfolioPosition.CurrentPrice;

                        // Рассчитываем доходность (0 для упрощения)
                        portfolioPosition.ExpectedYield = 0;

                        portfolioInfo.Positions.Add(portfolioPosition);
                    }

                    // Рассчитываем ожидаемую доходность (суммируем стоимость всех позиций)
                    portfolioInfo.ExpectedYield = portfolioInfo.Positions
                        .Sum(p => p.Balance * p.CurrentPrice * 0.01m); // Пример: 1% от стоимости
                }

                return portfolioInfo;
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
                var response = await SendRequest<GetAccountsResponse>(
                    "tinkoff.public.invest.api.contract.v1.UsersService/GetAccounts",
                    new { });

                if (response?.Accounts == null)
                    return new List<Account>();

                return response.Accounts.Select(a => new Account
                {
                    BrokerAccountType = GetAccountType(a.Type),
                    BrokerAccountId = a.Id
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения списка счетов: {ex.Message}", ex);
            }
        }

        public async Task<List<Operation>> GetOperationsAsync(DateTime from, DateTime to, string accountId = null)
        {
            EnsureConnected();

            try
            {
                var targetAccountId = accountId ?? _currentAccountId;

                var request = new
                {
                    accountId = targetAccountId,
                    from = FormatDate(from),
                    to = FormatDate(to),
                    state = "OPERATION_STATE_EXECUTED"
                };

                var response = await SendRequest<GetOperationsResponse>(
                    "tinkoff.public.invest.api.contract.v1.OperationsService/GetOperations",
                    request);

                if (response?.Operations == null || !response.Operations.Any())
                {
                    return new List<Operation>();
                }

                // Преобразуем операции API в нашу модель
                var operations = new List<Operation>();

                foreach (var apiOperation in response.Operations)
                {
                    var operation = await ConvertApiOperationToModel(apiOperation);
                    if (operation != null)
                    {
                        operations.Add(operation);
                    }
                }

                return operations.OrderByDescending(o => o.Date).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения операций: {ex.Message}", ex);
            }
        }

        public Task<bool> IsConnected() => Task.FromResult(_isConnected);

        public void Disconnect()
        {
            _isConnected = false;
            _apiKey = null;
            _currentAccountId = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _figiToTickerCache.Clear();
        }

        #region Token Persistence Methods

        public async Task<bool> TryConnectWithSavedTokenAsync()
        {
            try
            {
                var savedToken = await LoadTokenAsync();
                if (!string.IsNullOrEmpty(savedToken))
                {
                    return await ConnectAsync(savedToken);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task SaveTokenAsync(string apiKey)
        {
            try
            {
                // Создаем директорию если её нет
                var directory = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Шифруем токен (базовое шифрование для безопасности)
                var encryptedToken = SimpleEncrypt(apiKey);
                await File.WriteAllTextAsync(TokenFilePath, encryptedToken);

                // Устанавливаем скрытый атрибут файла (только на Windows)
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        File.SetAttributes(TokenFilePath, File.GetAttributes(TokenFilePath) | FileAttributes.Hidden);
                    }
                }
                catch
                {
                    // Игнорируем ошибки установки атрибутов
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения токена: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> HasSavedToken()
        {
            try
            {
                return File.Exists(TokenFilePath) &&
                       !string.IsNullOrEmpty(await LoadTokenAsync());
            }
            catch
            {
                return false;
            }
        }

        public async Task ClearSavedToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    File.Delete(TokenFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления токена: {ex.Message}");
            }
        }

        private async Task<string> LoadTokenAsync()
        {
            try
            {
                if (!File.Exists(TokenFilePath))
                    return null;

                var encryptedToken = await File.ReadAllTextAsync(TokenFilePath);
                return SimpleDecrypt(encryptedToken);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Private Methods

        private void EnsureConnected()
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Не подключено к Tinkoff API. Сначала выполните ConnectAsync.");
        }

        private async Task<string> GetTickerForFigi(string figi)
        {
            if (string.IsNullOrEmpty(figi))
                return "";

            // Проверяем кэш
            if (_figiToTickerCache.TryGetValue(figi, out string cachedTicker))
                return cachedTicker;

            try
            {
                var instrumentInfo = await GetInstrumentInfoByFigi(figi);
                if (!string.IsNullOrEmpty(instrumentInfo.Ticker))
                {
                    _figiToTickerCache[figi] = instrumentInfo.Ticker;
                    return instrumentInfo.Ticker;
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки получения тикера
            }

            return "";
        }

        private async Task<string> GetInstrumentName(string figi)
        {
            if (string.IsNullOrEmpty(figi))
                return "";

            try
            {
                var instrumentInfo = await GetInstrumentInfoByFigi(figi);
                return instrumentInfo.Name;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private async Task<InstrumentInfo> GetInstrumentInfoByFigi(string figi)
        {
            try
            {
                if (string.IsNullOrEmpty(figi))
                    return new InstrumentInfo();

                var request = new
                {
                    idType = "INSTRUMENT_ID_TYPE_FIGI",
                    classCode = "",
                    id = figi
                };

                var response = await SendRequest<InstrumentByResponse>(
                    "tinkoff.public.invest.api.contract.v1.InstrumentsService/GetInstrumentBy",
                    request);

                return new InstrumentInfo
                {
                    Ticker = response?.Instrument?.Ticker ?? "",
                    Name = response?.Instrument?.Name ?? "",
                    InstrumentType = response?.Instrument?.InstrumentType ?? ""
                };
            }
            catch (Exception)
            {
                return new InstrumentInfo();
            }
        }

        private async Task<Operation> ConvertApiOperationToModel(ApiOperation apiOperation)
        {
            try
            {
                var operation = new Operation
                {
                    Id = apiOperation.Id,
                    Date = apiOperation.Date.ToLocalTime(),
                    OperationType = GetOperationTypeName(apiOperation.OperationType),
                    Quantity = apiOperation.Quantity,
                    Price = apiOperation.Price?.ToDecimal() ?? 0,
                    Payment = apiOperation.Payment?.ToDecimal() ?? 0,
                    Currency = apiOperation.Currency ?? "RUB",
                    Status = GetOperationStateName(apiOperation.State)
                };

                // Получаем информацию об инструменте по FIGI
                if (!string.IsNullOrEmpty(apiOperation.Figi))
                {
                    var instrumentInfo = await GetInstrumentInfoByFigi(apiOperation.Figi);
                    operation.Ticker = instrumentInfo.Ticker;
                    operation.Name = instrumentInfo.Name;
                    operation.InstrumentType = GetInstrumentTypeName(instrumentInfo.InstrumentType ?? apiOperation.InstrumentType);
                }
                else
                {
                    // Для денежных операций
                    operation.Ticker = GetCurrencySymbol(operation.Currency);
                    operation.Name = GetOperationDescription(apiOperation.OperationType, operation.Currency);
                    operation.InstrumentType = "Денежная операция";
                }

                return operation;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        private string GetOperationTypeName(string type)
        {
            return type switch
            {
                "OPERATION_TYPE_BUY" => "Покупка",
                "OPERATION_TYPE_SELL" => "Продажа",
                "OPERATION_TYPE_DIVIDEND" => "Дивиденды",
                "OPERATION_TYPE_COUPON" => "Купон",
                "OPERATION_TYPE_BROKER_FEE" => "Комиссия",
                "OPERATION_TYPE_INPUT" => "Пополнение",
                "OPERATION_TYPE_OUTPUT" => "Вывод",
                "OPERATION_TYPE_TAX" => "Налог",
                _ => type?.Replace("OPERATION_TYPE_", "") ?? "Операция"
            };
        }

        private string GetOperationStateName(string state)
        {
            return state switch
            {
                "OPERATION_STATE_EXECUTED" => "Исполнена",
                "OPERATION_STATE_CANCELED" => "Отменена",
                "OPERATION_STATE_PROGRESS" => "В процессе",
                _ => state?.Replace("OPERATION_STATE_", "") ?? "Неизвестно"
            };
        }

        private string GetCurrencySymbol(string currency)
        {
            return currency?.ToUpper() switch
            {
                "RUB" => "₽",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                _ => currency ?? "RUB"
            };
        }

        private string GetOperationDescription(string operationType, string currency)
        {
            return operationType switch
            {
                "OPERATION_TYPE_INPUT" => $"Пополнение ({currency})",
                "OPERATION_TYPE_OUTPUT" => $"Вывод ({currency})",
                "OPERATION_TYPE_BROKER_FEE" => $"Брокерская комиссия ({currency})",
                "OPERATION_TYPE_TAX" => $"Налог ({currency})",
                _ => $"Операция ({currency})"
            };
        }

        private string GetInstrumentTypeName(string type)
        {
            return type?.ToLower() switch
            {
                "share" or "stock" => "Акция",
                "bond" => "Облигация",
                "etf" => "Фонд",
                "currency" => "Валюта",
                "future" => "Фьючерс",
                _ => type ?? "Инструмент"
            };
        }

        private string GetAccountType(string type)
        {
            return type switch
            {
                "ACCOUNT_TYPE_TINKOFF" => "Брокерский счет Tinkoff",
                "ACCOUNT_TYPE_TINKOFF_IIS" => "ИИС Tinkoff",
                "ACCOUNT_TYPE_INVEST_BOX" => "Инвесткопилка",
                _ => type
            };
        }

        private string GetAccountStatus(string status)
        {
            return status switch
            {
                "ACCOUNT_STATUS_NEW" => "Новый",
                "ACCOUNT_STATUS_OPEN" => "Открыт",
                "ACCOUNT_STATUS_CLOSED" => "Закрыт",
                _ => status
            };
        }

        // Простое шифрование для базовой безопасности
        private string SimpleEncrypt(string input)
        {
            try
            {
                // Простая XOR шифровка с ключом
                var key = "TabbedAppDemo2024!";
                var result = new char[input.Length];

                for (int i = 0; i < input.Length; i++)
                {
                    result[i] = (char)(input[i] ^ key[i % key.Length]);
                }

                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(result)));
            }
            catch
            {
                return input;
            }
        }

        private string SimpleDecrypt(string input)
        {
            try
            {
                var bytes = Convert.FromBase64String(input);
                var encrypted = System.Text.Encoding.UTF8.GetString(bytes);

                var key = "TabbedAppDemo2024!";
                var result = new char[encrypted.Length];

                for (int i = 0; i < encrypted.Length; i++)
                {
                    result[i] = (char)(encrypted[i] ^ key[i % key.Length]);
                }

                return new string(result);
            }
            catch
            {
                return input;
            }
        }

        private async Task<T> SendRequest<T>(string method, object request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(method, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка API ({response.StatusCode}): {errorContent}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseString, _jsonOptions);
        }

        #endregion

        #region Response Models (из тестовой программы)

        public class GetInfoResponse
        {
            [JsonConverter(typeof(BooleanConverter))]
            public bool PremStatus { get; set; }

            [JsonConverter(typeof(BooleanConverter))]
            public bool QualStatus { get; set; }

            public List<string> QualifiedForWorkWith { get; set; } = new List<string>();
            public string Tariff { get; set; } = "";
            public string UserId { get; set; } = "";
            public string RiskLevelCode { get; set; } = "";
        }

        public class GetAccountsResponse
        {
            public List<ApiAccount> Accounts { get; set; } = new List<ApiAccount>();
        }

        public class ApiAccount
        {
            public string Id { get; set; } = "";
            public string Type { get; set; } = "";
            public string Name { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime OpenedDate { get; set; }
            public DateTime ClosedDate { get; set; }
        }

        public class GetPortfolioResponse
        {
            public MoneyValue TotalAmountPortfolio { get; set; } = new MoneyValue();
            public List<ApiPortfolioPosition> Positions { get; set; } = new List<ApiPortfolioPosition>();
        }

        public class ApiPortfolioPosition
        {
            public string Figi { get; set; } = "";
            public string InstrumentType { get; set; } = "";
            public Quotation Quantity { get; set; } = new Quotation();
            public MoneyValue CurrentPrice { get; set; } = new MoneyValue();
        }

        public class GetOperationsResponse
        {
            public List<ApiOperation> Operations { get; set; } = new List<ApiOperation>();
        }

        public class ApiOperation
        {
            public string Id { get; set; } = "";
            public string ParentOperationId { get; set; } = "";
            public string Currency { get; set; } = "";
            public MoneyValue Payment { get; set; } = new MoneyValue();
            public MoneyValue Price { get; set; } = new MoneyValue();
            public string State { get; set; } = "";
            public long Quantity { get; set; }
            public long QuantityRest { get; set; }
            public string Figi { get; set; } = "";
            public string InstrumentType { get; set; } = "";
            public DateTime Date { get; set; }
            public string OperationType { get; set; } = "";
            public List<OperationTrade> Trades { get; set; } = new List<OperationTrade>();
            public string AssetUid { get; set; } = "";
            public string PositionUid { get; set; } = "";
            public string InstrumentUid { get; set; } = "";
        }

        public class OperationTrade
        {
            public string TradeId { get; set; } = "";
            public DateTime DateTime { get; set; }
            public long Quantity { get; set; }
            public MoneyValue Price { get; set; } = new MoneyValue();
        }

        public class InstrumentByResponse
        {
            public ApiInstrument Instrument { get; set; } = new ApiInstrument();
        }

        public class ApiInstrument
        {
            public string Figi { get; set; } = "";
            public string Ticker { get; set; } = "";
            public string Name { get; set; } = "";
            public string Currency { get; set; } = "";
            public int Lot { get; set; }
            public string InstrumentType { get; set; } = "";
        }

        public class MoneyValue
        {
            public string Currency { get; set; } = "";
            public Quotation UnitsNano { get; set; } = new Quotation();

            public decimal ToDecimal()
            {
                return UnitsNano.ToDecimal();
            }
        }

        public class Quotation
        {
            [JsonConverter(typeof(StringLongConverter))]
            public long Units { get; set; }

            [JsonConverter(typeof(StringLongConverter))]
            public long Nano { get; set; }

            public decimal ToDecimal()
            {
                return Units + (Nano / 1_000_000_000m);
            }
        }

        #endregion
    }

    #region Converters

    public class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => bool.TryParse(reader.GetString(), out bool result) && result,
                JsonTokenType.Number => reader.GetInt32() != 0,
                _ => false
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public class StringLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return long.TryParse(reader.GetString(), out long result) ? result : 0;
            }
            return reader.TryGetInt64(out long value) ? value : 0;
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public class QuotationConverter : JsonConverter<MoneyValue>
    {
        public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var moneyValue = new MoneyValue();

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case "currency":
                                moneyValue.Currency = reader.GetString() ?? "";
                                break;
                            case "units":
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    moneyValue.UnitsNano.Units = long.TryParse(reader.GetString(), out long units) ? units : 0;
                                }
                                else if (reader.TokenType == JsonTokenType.Number)
                                {
                                    moneyValue.UnitsNano.Units = reader.TryGetInt64(out long units) ? units : 0;
                                }
                                break;
                            case "nano":
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    moneyValue.UnitsNano.Nano = long.TryParse(reader.GetString(), out long nano) ? nano : 0;
                                }
                                else if (reader.TokenType == JsonTokenType.Number)
                                {
                                    moneyValue.UnitsNano.Nano = reader.TryGetInt64(out long nano) ? nano : 0;
                                }
                                break;
                        }
                    }
                }
            }

            return moneyValue;
        }

        public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("currency", value.Currency);
            writer.WriteNumber("units", value.UnitsNano.Units);
            writer.WriteNumber("nano", value.UnitsNano.Nano);
            writer.WriteEndObject();
        }
    }

    #endregion
}