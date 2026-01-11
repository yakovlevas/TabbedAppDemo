using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab4ViewModel : ObservableObject
    {
        private readonly ITinkoffApiService _tinkoffService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConnect))]
        [NotifyPropertyChangedFor(nameof(ConnectionColor))]
        private string _apiKey = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionColor))]
        private string _connectionStatus = "Не подключено";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsConnected))]
        [NotifyPropertyChangedFor(nameof(ConnectionColor))]
        [NotifyPropertyChangedFor(nameof(CanGetInfo))]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConnect))]
        [NotifyPropertyChangedFor(nameof(CanGetInfo))]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _accountInfoText = "Информация о счёте не загружена";

        [ObservableProperty]
        private string _portfolioInfoText = "Портфель не загружен";

        [ObservableProperty]
        private decimal _totalPortfolioValue;

        [ObservableProperty]
        private decimal _expectedYield;

        [ObservableProperty]
        private bool _hasSavedToken = false;

        [ObservableProperty]
        private bool _isCheckingSavedToken = false;

        // Вычисляемые свойства
        public bool IsConnected => _isAuthenticated;
        public Color ConnectionColor => _isAuthenticated ? Colors.Green : Colors.Red;
        public bool CanConnect => !_isLoading && !string.IsNullOrWhiteSpace(_apiKey);
        public bool CanGetInfo => _isAuthenticated && !_isLoading;

        public Tab4ViewModel(ITinkoffApiService tinkoffService, IDialogService dialogService)
        {
            _tinkoffService = tinkoffService;
            _dialogService = dialogService;

            // При создании ViewModel проверяем сохранённый токен
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                IsCheckingSavedToken = true;

                // Проверяем наличие сохранённого токена
                HasSavedToken = await _tinkoffService.HasSavedToken();

                // Если есть сохранённый токен, предлагаем использовать его
                if (HasSavedToken)
                {
                    // Даем время на загрузку интерфейса
                    await Task.Delay(1500);

                    var useSaved = await _dialogService.ShowConfirmationAsync(
                        "Сохранённый токен",
                        "Обнаружен сохранённый токен Tinkoff API.\n\n" +
                        "Хотите использовать его для автоматического подключения?",
                        "Подключиться", "Ввести новый");

                    if (useSaved)
                    {
                        await TryConnectWithSavedTokenAsync();
                    }
                    else
                    {
                        // Предлагаем удалить сохранённый токен
                        var deleteToken = await _dialogService.ShowConfirmationAsync(
                            "Удаление токена",
                            "Хотите удалить сохранённый токен?",
                            "Удалить", "Оставить");

                        if (deleteToken)
                        {
                            await ClearSavedTokenCommand();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
            finally
            {
                IsCheckingSavedToken = false;
            }
        }

        [RelayCommand]
        private async Task ConnectToTinkoff()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Введите API ключ Tinkoff Invest", "OK");
                return;
            }

            IsLoading = true;
            ConnectionStatus = "Подключение...";

            try
            {
                var success = await _tinkoffService.ConnectAsync(ApiKey);

                if (success)
                {
                    IsAuthenticated = true;
                    ConnectionStatus = "Подключено ✓";

                    // Автоматически загружаем базовую информацию
                    await LoadAccountInfo();

                    // Предлагаем сохранить токен только если он ещё не сохранён
                    if (!HasSavedToken)
                    {
                        await AskToSaveToken(ApiKey);
                    }

                    await _dialogService.ShowAlertAsync("Успех",
                        "Успешное подключение к Tinkoff Invest API", "OK");
                }
                else
                {
                    ConnectionStatus = "Ошибка подключения";
                    await _dialogService.ShowAlertAsync("Ошибка",
                        "Не удалось подключиться к Tinkoff API", "OK");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Ошибка";
                await _dialogService.ShowAlertAsync("Ошибка подключения",
                    $"Ошибка: {ex.Message}\n\nПроверьте:\n1. Корректность API ключа\n2. Подключение к интернету\n3. Активность счёта Tinkoff Invest",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UseSavedToken()
        {
            if (!HasSavedToken)
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Сохранённый токен не найден", "OK");
                return;
            }

            await TryConnectWithSavedTokenAsync();
        }

        private async Task TryConnectWithSavedTokenAsync()
        {
            IsLoading = true;
            ConnectionStatus = "Подключение с сохранённым токеном...";

            try
            {
                var success = await _tinkoffService.TryConnectWithSavedTokenAsync();
                if (success)
                {
                    IsAuthenticated = true;
                    ConnectionStatus = "Подключено (сохранённый токен) ✓";

                    // Загружаем информацию о счете
                    await LoadAccountInfo();

                    // Получаем информацию о счетах для отображения
                    var accounts = await _tinkoffService.GetAccountsAsync();
                    if (accounts.Any())
                    {
                        // Показываем маскированный токен
                        var accountId = accounts[0].BrokerAccountId;
                        var maskedId = accountId.Length > 4
                            ? "••••••••" + accountId.Substring(accountId.Length - 4)
                            : accountId;
                        ApiKey = maskedId;
                    }

                    await _dialogService.ShowAlertAsync("Успех",
                        "Успешное подключение с сохранённым токеном", "OK");
                }
                else
                {
                    ConnectionStatus = "Ошибка подключения";
                    await _dialogService.ShowAlertAsync("Ошибка",
                        "Не удалось подключиться с сохранённым токеном.\n\nВозможно, токен устарел или недействителен.",
                        "OK");

                    // Предлагаем удалить нерабочий токен
                    var deleteToken = await _dialogService.ShowConfirmationAsync(
                        "Недействительный токен",
                        "Сохранённый токен не работает. Удалить его?",
                        "Удалить", "Оставить");

                    if (deleteToken)
                    {
                        await ClearSavedTokenCommand();
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Ошибка";
                await _dialogService.ShowAlertAsync("Ошибка подключения",
                    $"Не удалось подключиться: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AskToSaveToken(string apiKey)
        {
            try
            {
                var saveToken = await _dialogService.ShowConfirmationAsync(
                    "Сохранение токена",
                    "Сохранить API-токен для последующего использования?\n\n" +
                    "⚠️ Токен будет сохранён в зашифрованном виде на устройстве.\n" +
                    "⚠️ Не сохраняйте токен на общедоступных устройствах.",
                    "Сохранить", "Не сохранять");

                if (saveToken)
                {
                    await _tinkoffService.SaveTokenAsync(apiKey);
                    HasSavedToken = true;
                    await _dialogService.ShowAlertAsync("Сохранено",
                        "Токен успешно сохранён", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении токена: {ex.Message}");
                await _dialogService.ShowAlertAsync("Ошибка",
                    "Не удалось сохранить токен", "OK");
            }
        }

        [RelayCommand]
        private async Task ClearSavedTokenCommand()
        {
            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Удаление токена",
                    "Вы уверены, что хотите удалить сохранённый токен?\n\n" +
                    "После удаления потребуется повторно ввести токен для подключения.",
                    "Удалить", "Отмена");

                if (confirm)
                {
                    await _tinkoffService.ClearSavedToken();
                    HasSavedToken = false;

                    // Также очищаем текущее подключение если оно было с сохранённым токеном
                    if (IsAuthenticated && string.IsNullOrEmpty(ApiKey) || ApiKey.Contains("••••"))
                    {
                        ClearConnection();
                    }

                    await _dialogService.ShowAlertAsync("Успех",
                        "Сохранённый токен удалён", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось удалить токен: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task GetAccountInfo()
        {
            await LoadAccountInfo();
        }

        [RelayCommand]
        private async Task GetPortfolioInfo()
        {
            if (!IsAuthenticated)
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Сначала подключитесь к Tinkoff API", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var portfolio = await _tinkoffService.GetPortfolioAsync();

                TotalPortfolioValue = portfolio.TotalPortfolioValue;
                ExpectedYield = portfolio.ExpectedYield;

                PortfolioInfoText = $"💰 Стоимость портфеля: {TotalPortfolioValue:C}\n" +
                                   $"📈 Ожидаемая доходность: {ExpectedYield:C}\n" +
                                   $"📊 Позиций: {portfolio.Positions.Count}\n" +
                                   $"💱 Валюта: {portfolio.Currency}";

                // Показываем детали портфеля
                if (portfolio.Positions.Any())
                {
                    var positionsText = "📋 Детали портфеля:\n\n";

                    foreach (var position in portfolio.Positions)
                    {
                        var tickerDisplay = !string.IsNullOrEmpty(position.Ticker)
                            ? $"{position.Ticker}"
                            : position.Figi;

                        var nameDisplay = !string.IsNullOrEmpty(position.Name)
                            ? position.Name
                            : "Неизвестный инструмент";

                        var positionValue = position.Balance * position.CurrentPrice;

                        positionsText += $"🏷️  {tickerDisplay} ({nameDisplay})\n" +
                                       $"   Кол-во: {position.Balance:N2}\n" +
                                       $"   Ср. цена: {position.AveragePositionPrice:C}\n" +
                                       $"   Текущая цена: {position.CurrentPrice:C}\n" +
                                       $"   Стоимость: {positionValue:C}\n" +
                                       $"   Доходность: {position.ExpectedYield:C}\n\n";
                    }

                    await _dialogService.ShowAlertAsync("Детали портфеля", positionsText, "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Портфель", "Портфель пуст", "OK");
                }
            }
            catch (Exception ex)
            {
                PortfolioInfoText = "❌ Ошибка загрузки портфеля";
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось загрузить портфель: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearConnection()
        {
            _tinkoffService.Disconnect();
            ApiKey = "";
            IsAuthenticated = false;
            ConnectionStatus = "Не подключено";
            AccountInfoText = "Информация о счёте не загружена";
            PortfolioInfoText = "Портфель не загружен";
            TotalPortfolioValue = 0;
            ExpectedYield = 0;
        }

        [RelayCommand]
        private async Task ShowApiHelp()
        {
            await _dialogService.ShowAlertAsync("Как получить API ключ",
                "1. Откройте приложение Tinkoff Инвестиции\n" +
                "2. Перейдите в Настройки → Для разработчиков\n" +
                "3. Нажмите 'Токен для OpenAPI'\n" +
                "4. Скопируйте токен и вставьте в поле выше\n\n" +
                "⚠️ Не делитесь токеном с посторонними!\n" +
                "⚠️ Сохраняйте токен только на доверенных устройствах\n" +
                "⚠️ Токен можно отозвать в любой момент в настройках приложения",
                "Понятно");
        }

        [RelayCommand]
        private async Task ShowTokenInfo()
        {
            if (!HasSavedToken)
            {
                await _dialogService.ShowAlertAsync("Информация",
                    "Сохранённый токен не найден.\n\n" +
                    "После успешного подключения вы сможете сохранить токен для последующего использования.",
                    "OK");
                return;
            }

            var tokenStatus = IsAuthenticated ? "используется" : "сохранён, но не подключён";

            await _dialogService.ShowAlertAsync("Сохранённый токен",
                $"📱 Статус: {tokenStatus}\n\n" +
                $"Токен сохранён в зашифрованном виде на устройстве.\n" +
                $"Для удаления токена нажмите кнопку 'Удалить сохранённый токен'.",
                "OK");
        }

        private async Task LoadAccountInfo()
        {
            if (!IsAuthenticated) return;

            IsLoading = true;

            try
            {
                var info = await _tinkoffService.GetAccountInfoAsync();

                AccountInfoText = $"🏦 Счёт: {info.BrokerAccountId}\n" +
                                 $"📋 Тип: {info.BrokerAccountType}\n" +
                                 $"📊 Статус: {info.Status}\n" +
                                 $"💰 Баланс: {info.TotalBalance:C}\n" +
                                 $"📈 Доходность: {info.ExpectedYield:C}\n" +
                                 $"📂 Всего счетов: {info.TotalAccounts}\n" +
                                 $"🕒 Обновлено: {info.LastUpdate:HH:mm:ss}";

                // Обновляем общие значения
                TotalPortfolioValue = info.TotalBalance ?? 0;
                ExpectedYield = info.ExpectedYield ?? 0;

                await _dialogService.ShowAlertAsync("Информация о счёте", AccountInfoText, "OK");
            }
            catch (Exception ex)
            {
                AccountInfoText = "❌ Ошибка загрузки информации";
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось получить информацию: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}