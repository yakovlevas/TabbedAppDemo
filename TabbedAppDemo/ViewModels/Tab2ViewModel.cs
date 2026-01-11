using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab2ViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = "Tab 2 - MVVM Version";

        [ObservableProperty]
        private string _description = "This tab is now using MVVM pattern";

        [ObservableProperty]
        private int _sliderValue = 50;

        [ObservableProperty]
        private bool _isToggleOn = true;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Now;

        [RelayCommand]
        private async Task ShowTab2Message()
        {
            await Application.Current.MainPage.DisplayAlert("Tab 2",
                $"Slider: {SliderValue}\nToggle: {IsToggleOn}\nDate: {SelectedDate:d}",
                "OK");
        }

        [RelayCommand]
        private void ResetValues()
        {
            SliderValue = 50;
            IsToggleOn = true;
            SelectedDate = DateTime.Now;
        }
    }
}