using System;
using System.Threading.Tasks;

namespace TabbedAppDemo.Services
{
    public class SimpleConnectionStateService : IConnectionStateService
    {
        private bool _isConnected = false; // Явно инициализируем как false
        private readonly object _lock = new object();

        public bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    return _isConnected;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _isConnected = value;
                }
            }
        }

        public event EventHandler<bool> ConnectionChanged;

        public async Task SetConnectedAsync(bool isConnected)
        {
            if (IsConnected == isConnected)
                return;

            Console.WriteLine($"[ConnectionStateService] Изменение статуса: {IsConnected} -> {isConnected}");
            IsConnected = isConnected;

            // Вызываем событие безопасно
            var handler = ConnectionChanged;
            if (handler != null)
            {
                // Используем Device.BeginInvokeOnMainThread для MAUI
                Microsoft.Maui.Controls.Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        Console.WriteLine($"[ConnectionStateService] Вызываем ConnectionChanged: {isConnected}");
                        handler.Invoke(this, isConnected);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при вызове обработчика: {ex.Message}");
                    }
                });
            }

            await Task.CompletedTask;
        }

        public bool GetCurrentStatus()
        {
            return IsConnected;
        }
    }
}