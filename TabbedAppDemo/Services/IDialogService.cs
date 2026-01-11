using System.Threading.Tasks;

namespace TabbedAppDemo.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Показать диалоговое окно с одной кнопкой (Alert)
        /// </summary>
        Task ShowAlertAsync(string title, string message, string cancel);

        /// <summary>
        /// Показать диалоговое окно с подтверждением (две кнопки)
        /// </summary>
        Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);

        /// <summary>
        /// Показать диалоговое окно с ошибкой
        /// </summary>
        Task ShowErrorAsync(string message, string title = "Ошибка");

        /// <summary>
        /// Показать диалоговое окно с успехом
        /// </summary>
        Task ShowSuccessAsync(string message, string title = "Успех");

        /// <summary>
        /// Показать диалоговое окно с предупреждением
        /// </summary>
        Task ShowWarningAsync(string message, string title = "Внимание");

        /// <summary>
        /// Показать диалоговое окно с информацией
        /// </summary>
        Task ShowInfoAsync(string message, string title = "Информация");

        /// <summary>
        /// Показать toast-сообщение
        /// </summary>
        Task ShowToastAsync(string message, int duration = 3);
    }
}