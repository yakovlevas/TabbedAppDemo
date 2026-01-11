using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab1ViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private int _count = 0;

        [ObservableProperty]
        private string _counterText = "Current count: 0";

        [ObservableProperty]
        private string _welcomeMessage = "Hello from Tab1 MVVM!";

        // Конструктор без параметров для XAML
        public Tab1ViewModel()
        {
            // Используем временную реализацию
            _dialogService = new Services.DialogService();
        }

        // Конструктор с DI (для использования с Dependency Injection)
        public Tab1ViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        [RelayCommand]
        private void IncrementCounter()
        {
            Count++;

            if (Count == 1)
                CounterText = $"Clicked {Count} time";
            else
                CounterText = $"Clicked {Count} times";
        }

        [RelayCommand]
        private async Task ShowHelloWorld()
        {
            await _dialogService.ShowAlertAsync("Сообщение", "Привет Мир из Tab1!", "OK");
        }
    }
}