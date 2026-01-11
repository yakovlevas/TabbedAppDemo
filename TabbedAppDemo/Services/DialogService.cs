// Services/DialogService.cs
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace TabbedAppDemo.Services
{
    public class DialogService : IDialogService
    {
        public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
            }
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message,
                                                     string accept = "Yes", string cancel = "No")
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
            }
            return false;
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel = null,
                                                      string destruction = null, params string[] buttons)
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons);
            }
            return string.Empty;
        }

        // Реализация метода с int параметром
        public async Task ShowToastAsync(string message, int durationMs = 3000)
        {
            try
            {
                // Конвертируем миллисекунды в ToastDuration
                var toastDuration = durationMs <= 2000 ? ToastDuration.Short : ToastDuration.Long;

                var toast = Toast.Make(message, toastDuration, 14);
                await toast.Show();
            }
            catch
            {
                // Fallback на DisplayAlert если Toast не доступен
                await ShowAlertAsync("", message, "OK");
            }
        }
    }
}