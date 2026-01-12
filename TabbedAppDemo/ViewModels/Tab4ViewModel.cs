using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;
using System.Diagnostics;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab4ViewModel : ObservableObject
    {
        private readonly ITinkoffApiService _tinkoffService;
        private readonly IDialogService _dialogService;
        private readonly IConnectionStateService _connectionState;
        private bool _isPageActive = false;
        private bool _isSettingToggle = false; // Флаг для предотвращения рекурсии

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanTestConnection))]
        private string _apiKey = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionColor))]
        private string _connectionStatus = "Не подключено";

        // Реальное состояние подключения к API
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsConnected))]
        [NotifyPropertyChangedFor(nameof(ConnectionColor))]
        [NotifyPropertyChangedFor(nameof(CanSaveToken))]
        private bool _isAuthenticated = false;

        // Состояние переключателя в UI
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanTestConnection))]
        private bool _isConnectionToggleEnabled = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanTestConnection))]
        [NotifyPropertyChangedFor(nameof(CanSaveToken))]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _hasSavedToken = false;

        // Вычисляемые свойства
        public bool IsConnected => _isAuthenticated;
        public Color ConnectionColor => _isAuthenticated ? Colors.Green : Colors.Red;
        public bool CanTestConnection => !_isLoading && !string.IsNullOrWhiteSpace(_apiKey);
        public bool CanSaveToken => _isAuthenticated && !_isLoading && !_hasSavedToken;

        public Tab4ViewModel(
            ITinkoffApiService tinkoffService,
            IDialogService dialogService,
            IConnectionStateService connectionState)
        {
            _tinkoffService = tinkoffService;
            _dialogService = dialogService;
            _connectionState = connectionState;

            Debug.WriteLine("[Tab4ViewModel] Конструктор вызван");
        }

        // Обработчик изменения состояния переключателя
        partial void OnIsConnectionToggleEnabledChanged(bool value)
        {
            if (_isPageActive && !_isSettingToggle)
            {
                Debug.WriteLine($"[Tab4ViewModel] Переключатель изменен: {value}");

                // Запускаем команду переключения подключения
                Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ToggleConnectionCommand.ExecuteAsync(null);
                    });
                });
            }
        }

        // Метод для вызова при появлении страницы
        public void OnAppearing()
        {
            _isPageActive = true;
            Debug.WriteLine("[Tab4ViewModel] OnAppearing: страница активна");
        }

        // Метод для вызова при скрытии страницы
        public void OnDisappearing()
        {
            _isPageActive = false;
            Debug.WriteLine("[Tab4ViewModel] OnDisappearing: страница неактивна");
        }

        [RelayCommand]
        private async Task ToggleConnection()
        {
            if (!_isPageActive) return;

            // Синхронизируем переключатель с реальным состоянием
            if (IsConnectionToggleEnabled == IsAuthenticated)
            {
                Debug.WriteLine($"[Tab4ViewModel] Состояния синхронизированы: UI={IsConnectionToggleEnabled}, Real={IsAuthenticated}");
                return;
            }

            if (!IsAuthenticated && string.IsNullOrWhiteSpace(ApiKey))
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Введите API ключ", "OK");

                // Сбрасываем переключатель обратно
                _isSettingToggle = true;
                IsConnectionToggleEnabled = false;
                _isSettingToggle = false;
                return;
            }

            IsLoading = true;

            try
            {
                if (IsAuthenticated)
                {
                    // Отключение
                    Debug.WriteLine("[Tab4ViewModel] Выполняем отключение...");
                    _tinkoffService.Disconnect();
                    IsAuthenticated = false;
                    ConnectionStatus = "Не подключено";

                    // Обновляем глобальное состояние подключения
                    await _connectionState.SetConnectedAsync(false);

                    // UI уже должен быть синхронизирован через TwoWay binding
                }
                else
                {
                    // Попытка подключения
                    Debug.WriteLine("[Tab4ViewModel] Выполняем подключение...");
                    ConnectionStatus = "Подключение...";

                    // ConnectAsync проверяет токен и сохраняет его в памяти сервиса
                    var success = await _tinkoffService.ConnectAsync(ApiKey);

                    if (success)
                    {
                        IsAuthenticated = true;
                        ConnectionStatus = "Подключено ✓";

                        // Обновляем глобальное состояние подключения
                        await _connectionState.SetConnectedAsync(true);

                        await _dialogService.ShowAlertAsync("Успех",
                            "Успешное подключение к Tinkoff Invest API", "OK");
                    }
                    else
                    {
                        IsAuthenticated = false;
                        ConnectionStatus = "Ошибка подключения";
                        await _dialogService.ShowAlertAsync("Ошибка",
                            "Не удалось подключиться к Tinkoff API", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;
                ConnectionStatus = "Ошибка";

                await _dialogService.ShowAlertAsync("Ошибка подключения",
                    $"Ошибка: {ex.Message}\n\nПроверьте:\n1. Корректность API ключа\n2. Подключение к интернету\n3. Активность счёта Tinkoff Invest",
                    "OK");
            }
            finally
            {
                // Синхронизируем UI переключатель с реальным состоянием
                _isSettingToggle = true;
                IsConnectionToggleEnabled = IsAuthenticated;
                _isSettingToggle = false;

                IsLoading = false;
                Debug.WriteLine($"[Tab4ViewModel] Завершено. Состояние: IsAuthenticated={IsAuthenticated}");
            }
        }

        [RelayCommand]
        private async Task LoadSavedToken()
        {
            if (!_isPageActive) return;

            try
            {
                Debug.WriteLine("[Tab4ViewModel] Загрузка сохраненного токена...");

                var token = await _tinkoffService.LoadTokenFromFile();

                if (!string.IsNullOrEmpty(token))
                {
                    ApiKey = token;
                    HasSavedToken = true;

                    await _dialogService.ShowAlertAsync("Успех",
                        "Сохраненный токен загружен в поле ввода", "OK");
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Информация",
                        "Сохраненный токен не найден", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось загрузить токен: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SaveToken()
        {
            if (!_isPageActive) return;

            if (!IsAuthenticated)
            {
                await _dialogService.ShowAlertAsync("Ошибка", "Сначала подключитесь к API", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                await _dialogService.ShowAlertAsync("Ошибка", "API ключ пустой", "OK");
                return;
            }

            try
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Сохранение токена",
                    "Сохранить текущий токен на устройстве?\n\n" +
                    "Токен будет сохранен в зашифрованном виде для последующего использования.",
                    "Сохранить", "Отмена");

                if (confirm)
                {
                    await _tinkoffService.SaveTokenToFile(ApiKey);
                    HasSavedToken = true;

                    await _dialogService.ShowAlertAsync("Успех",
                        "Токен успешно сохранен", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось сохранить токен: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ClearSavedToken()
        {
            if (!_isPageActive) return;

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

                    // Если мы используем сохраненный токен, отключаемся
                    if (IsAuthenticated && ApiKey.Contains("••••"))
                    {
                        await ToggleConnectionCommand.ExecuteAsync(null);
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
        private async Task ShowApiHelp()
        {
            if (!_isPageActive) return;

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
    }
}