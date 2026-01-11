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

        // Вычисляемые свойства
        public bool IsConnected => _isAuthenticated;
        public Color ConnectionColor => _isAuthenticated ? Colors.Green : Colors.Red;
        public bool CanConnect => !_isLoading && !string.IsNullOrWhiteSpace(_apiKey);
        public bool CanGetInfo => _isAuthenticated && !_isLoading;

        public Tab4ViewModel(ITinkoffApiService tinkoffService, IDialogService dialogService)
        {
            _tinkoffService = tinkoffService;
            _dialogService = dialogService;
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

                PortfolioInfoText = $"Стоимость портфеля: {TotalPortfolioValue:C}\n" +
                                   $"Ожидаемая доходность: {ExpectedYield:C}\n" +
                                   $"Позиций: {portfolio.Positions.Count}\n" +
                                   $"Валюта: {portfolio.Currency}";

                // Показываем детали портфеля
                if (portfolio.Positions.Any())
                {
                    var positionsText = string.Join("\n\n", portfolio.Positions.Select(p =>
                        $"{p.Ticker} ({p.Name})\n" +
                        $"Кол-во: {p.Balance}\n" +
                        $"Ср. цена: {p.AveragePositionPrice:C}\n" +
                        $"Текущая: {p.CurrentPrice:C}\n" +
                        $"Доходность: {p.ExpectedYield:C}"));

                    await _dialogService.ShowAlertAsync("Детали портфеля", positionsText, "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Портфель", "Портфель пуст", "OK");
                }
            }
            catch (Exception ex)
            {
                PortfolioInfoText = "Ошибка загрузки портфеля";
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
                "⚠️ Не делитесь токеном с посторонними!",
                "Понятно");
        }

        private async Task LoadAccountInfo()
        {
            if (!IsAuthenticated) return;

            IsLoading = true;

            try
            {
                var info = await _tinkoffService.GetAccountInfoAsync();

                AccountInfoText = $"Счёт: {info.BrokerAccountId}\n" +
                                 $"Тип: {info.BrokerAccountType}\n" +
                                 $"Статус: {info.Status}\n" +
                                 $"Баланс: {info.TotalBalance:C}\n" +
                                 $"Доходность: {info.ExpectedYield:C}\n" +
                                 $"Всего счетов: {info.TotalAccounts}\n" +
                                 $"Обновлено: {info.LastUpdate:HH:mm:ss}";

                // Обновляем общие значения
                TotalPortfolioValue = info.TotalBalance ?? 0;
                ExpectedYield = info.ExpectedYield ?? 0;

                await _dialogService.ShowAlertAsync("Информация о счёте", AccountInfoText, "OK");
            }
            catch (Exception ex)
            {
                AccountInfoText = "Ошибка загрузки информации";
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