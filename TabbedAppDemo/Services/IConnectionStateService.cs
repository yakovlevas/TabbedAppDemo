using System;

namespace TabbedAppDemo.Services
{
    public interface IConnectionStateService
    {
        /// <summary>
        /// Текущий статус подключения
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Событие изменения статуса подключения
        /// </summary>
        event EventHandler<bool> ConnectionChanged;

        /// <summary>
        /// Установить статус подключения и уведомить подписчиков
        /// </summary>
        Task SetConnectedAsync(bool isConnected);

        /// <summary>
        /// Получить текущий статус подключения
        /// </summary>
        bool GetCurrentStatus();
    }
}