using TabbedAppDemo.ViewModels;
using TabbedAppDemo.Services;

namespace TabbedAppDemo.Views
{
    public partial class Tab2Page : ContentPage
    {
        private Tab2ViewModel _viewModel;
        private readonly IConnectionStateService _connectionState;

        public Tab2Page(Tab2ViewModel viewModel, IConnectionStateService connectionState)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
            _connectionState = connectionState;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // При появлении страницы просто проверяем текущий статус из сервиса
            CheckCurrentConnectionStatus();
        }

        private void CheckCurrentConnectionStatus()
        {
            try
            {
                if (_viewModel != null)
                {
                    // Берем текущий статус из сервиса состояния
                    var currentStatus = _connectionState.GetCurrentStatus();

                    // Обновляем только если статус изменился
                    if (_viewModel.IsConnected != currentStatus)
                    {
                        _viewModel.IsConnected = currentStatus;

                        // Можно показать уведомление, если подключились
                        if (currentStatus && !_viewModel.HasOperations)
                        {
                            // Только уведомление, без автоматической загрузки
                            _ = _viewModel.ShowConnectionStatusToast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки подключения: {ex.Message}");
            }
        }

        // Обработчик для обновления при подключении (больше не нужен - используем сервис)
        // public async Task RefreshOnConnected()
        // {
        //     if (_viewModel != null)
        //     {
        //         _viewModel.IsConnected = true;
        //         await _viewModel.LoadOperationsAsync();
        //     }
        // }

        // Обработчик для сброса при отключении (обновляем)
        public void ResetOnDisconnected()
        {
            try
            {
                if (_viewModel != null)
                {
                    // Просто сбрасываем статус подключения
                    _viewModel.IsConnected = false;

                    // Очищаем операции (опционально)
                    _viewModel.Operations.Clear();
                    _viewModel.GroupedOperations.Clear();

                    // Сбрасываем статистику
                    _viewModel.TotalIncome = 0;
                    _viewModel.TotalExpense = 0;
                    _viewModel.NetResult = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сбросе: {ex.Message}");
            }
        }

        // Метод для принудительного обновления данных
        public async Task ForceRefresh()
        {
            if (_viewModel != null && _viewModel.IsConnected)
            {
                await _viewModel.LoadOperationsAsync();
            }
        }
    }
}