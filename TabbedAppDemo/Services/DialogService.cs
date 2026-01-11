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

        public async Task<string> ShowPromptAsync(string title, string message, string accept, string cancel, string placeholder = null, string initialValue = null)
        {
            if (Application.Current?.MainPage != null)
            {
                return await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayPromptAsync(
                        title,
                        message,
                        accept,
                        cancel,
                        placeholder,
                        maxLength: 500,
                        keyboard: Microsoft.Maui.Keyboard.Default,
                        initialValue: initialValue));
            }

            return null;
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

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            if (Application.Current?.MainPage != null)
            {
                return await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayActionSheet(
                        title,
                        cancel,
                        destruction,
                        buttons));
            }

            return null;
        }
    }
}