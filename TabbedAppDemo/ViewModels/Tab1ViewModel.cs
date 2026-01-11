// ViewModels/Tab1ViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab1ViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _welcomeMessage = "Hello from MVVM!";

        [RelayCommand]
        private void SayHello()
        {
            // Пока ничего не делаем - просто тест
            WelcomeMessage = $"Button clicked at {DateTime.Now:HH:mm:ss}";
        }
    }
}