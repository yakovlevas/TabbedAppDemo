using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab1ViewModel : ObservableObject
    {
        private int _count = 0;

        [ObservableProperty]
        private string _counterText = "Current count: 0";

        [RelayCommand]
        private void IncrementCounter()
        {
            _count++;

            if (_count == 1)
                CounterText = $"Clicked {_count} time";
            else
                CounterText = $"Clicked {_count} times";
        }

        [RelayCommand]
        private async Task ShowHelloWorld()
        {
            // В реальном MVVM лучше использовать сервис диалогов
            // но для простоты покажем напрямую
            // Это временное решение - в идеале вынести в сервис
            await Application.Current.MainPage.DisplayAlert("Сообщение", "Привет Мир!", "OK");
        }
    }
}