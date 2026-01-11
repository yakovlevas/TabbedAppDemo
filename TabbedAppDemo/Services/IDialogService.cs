using System.Threading.Tasks;

namespace TabbedAppDemo.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Показать диалоговое окно с одной кнопкой (Alert)
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="message">Сообщение</param>
        /// <param name="cancel">Текст кнопки отмены/закрытия</param>
        /// <returns>Task</returns>
        Task ShowAlertAsync(string title, string message, string cancel);

        /// <summary>
        /// Показать диалоговое окно с подтверждением (две кнопки)
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="message">Сообщение</param>
        /// <param name="accept">Текст кнопки подтверждения</param>
        /// <param name="cancel">Текст кнопки отмены</param>
        /// <returns>True если пользователь нажал кнопку подтверждения</returns>
        Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);

        /// <summary>
        /// Показать диалоговое окно с вводом текста
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="message">Сообщение</param>
        /// <param name="accept">Текст кнопки подтверждения</param>
        /// <param name="cancel">Текст кнопки отмены</param>
        /// <param name="placeholder">Подсказка в поле ввода</param>
        /// <param name="initialValue">Начальное значение</param>
        /// <returns>Введенный текст или null если отменено</returns>
        Task<string> ShowPromptAsync(string title, string message, string accept, string cancel, string placeholder = null, string initialValue = null);

        /// <summary>
        /// Показать toast-сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="duration">Длительность показа (секунды)</param>
        /// <returns>Task</returns>
        Task ShowToastAsync(string message, int duration = 3);

        /// <summary>
        /// Показать диалог выбора действия
        /// </summary>
        /// <param name="title">Заголовок</param>
        /// <param name="cancel">Текст кнопки отмены</param>
        /// <param name="destruction">Текст деструктивной кнопки</param>
        /// <param name="buttons">Массив кнопок для выбора</param>
        /// <returns>Выбранное действие или null если отменено</returns>
        Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons);
    }
}