// ViewModels/Tab1ViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab1ViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;

        public Tab1ViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        [RelayCommand]
        private async Task ShowHelloWorld()
        {
            // ХОРОШО: ViewModel не знает о UI деталях
            await _dialogService.ShowAlertAsync("Сообщение", "Привет Мир!", "OK");
        }
    }
}