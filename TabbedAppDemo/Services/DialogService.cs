using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;

namespace TabbedAppDemo.Services
{
    public class DialogService : IDialogService
    {
        public DialogService()
        {
            // Простой конструктор без зависимостей
        }

        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert(title, message, cancel));
            }
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            if (Application.Current?.MainPage != null)
            {
                return await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert(title, message, accept, cancel));
            }

            return false;
        }

        public async Task ShowErrorAsync(string message, string title = "Ошибка")
        {
            await ShowAlertAsync(title, $"❌ {message}", "OK");
        }

        public async Task ShowSuccessAsync(string message, string title = "Успех")
        {
            await ShowAlertAsync(title, $"✅ {message}", "OK");
        }

        public async Task ShowWarningAsync(string message, string title = "Внимание")
        {
            await ShowAlertAsync(title, $"⚠️ {message}", "OK");
        }

        public async Task ShowInfoAsync(string message, string title = "Информация")
        {
            await ShowAlertAsync(title, $"ℹ️ {message}", "OK");
        }

        public async Task ShowToastAsync(string message, int durationSeconds = 3)
        {
            try
            {
                // Конвертируем секунды в ToastDuration
                ToastDuration duration = durationSeconds <= 2 ? ToastDuration.Short : ToastDuration.Long;

                var toast = Toast.Make(message, duration);
                await MainThread.InvokeOnMainThreadAsync(() => toast.Show());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Toast error: {ex.Message}");
            }
        }
    }
}