// Services/IDialogService.cs
namespace TabbedAppDemo.Services
{
    public interface IDialogService
    {
        // Базовые методы
        Task ShowAlertAsync(string title, string message, string cancel = "OK");

        Task<bool> ShowConfirmationAsync(string title, string message,
                                        string accept = "Yes", string cancel = "No");

        Task<string> ShowActionSheetAsync(string title, string cancel = null,
                                         string destruction = null, params string[] buttons);

        // Toast уведомления - используем int для миллисекунд
        Task ShowToastAsync(string message, int durationMs = 3000);
    }
}