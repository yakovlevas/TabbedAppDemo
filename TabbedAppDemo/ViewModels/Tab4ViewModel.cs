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
        private string _apiKey = "";

        [ObservableProperty]
        private string _connectionStatus = "Не подключено";

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _accountInfoText = "Информация о счёте не загружена";

        // Вычисляемые свойства
        public Color ConnectionColor => IsConnected ? Colors.Green : Colors.Red;
        public bool CanConnect => !IsLoading;

        // Важно: ТОЛЬКО ОДИН конструктор с DI
        public Tab4ViewModel(ITinkoffApiService tinkoffService, IDialogService dialogService)
        {
            _tinkoffService = tinkoffService;
            _dialogService = dialogService;
        }

        // Уведомляем об изменении вычисляемых свойств
        partial void OnIsConnectedChanged(bool value) => OnPropertyChanged(nameof(ConnectionColor));
        partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanConnect));

        [RelayCommand]
        private async Task ConnectToTinkoff()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Введите API ключ", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var success = await _tinkoffService.ConnectAsync(ApiKey);

                if (success)
                {
                    IsConnected = true;
                    ConnectionStatus = "Подключено ✓";
                    await _dialogService.ShowAlertAsync("Успех", "Подключение к Tinkoff API установлено", "OK");
                }
                else
                {
                    ConnectionStatus = "Ошибка подключения";
                    await _dialogService.ShowAlertAsync("Ошибка", "Не удалось подключиться к Tinkoff API", "OK");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Ошибка";
                await _dialogService.ShowAlertAsync("Ошибка", $"Ошибка подключения: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GetAccountInfo()
        {
            if (!IsConnected)
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Сначала подключитесь к Tinkoff API", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var info = await _tinkoffService.GetAccountInfoAsync();

                AccountInfoText = $"Счёт: {info.BrokerAccountId}\n" +
                                 $"Статус: {info.Status}\n" +
                                 $"Баланс: {info.TotalBalance:C}\n" +
                                 $"Обновлено: {info.LastUpdate:HH:mm:ss}";

                await _dialogService.ShowAlertAsync("Информация о счёте", AccountInfoText, "OK");
            }
            catch (Exception ex)
            {
                AccountInfoText = "Ошибка загрузки информации";
                await _dialogService.ShowAlertAsync("Ошибка", $"Не удалось получить информацию: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearApiKey()
        {
            ApiKey = "";
            IsConnected = false;
            ConnectionStatus = "Не подключено";
            AccountInfoText = "Информация о счёте не загружена";
        }
    }
}