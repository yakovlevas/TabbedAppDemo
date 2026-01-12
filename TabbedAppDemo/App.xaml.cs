using System.Diagnostics;

namespace TabbedAppDemo
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Настройка глобального обработчика исключений
            SetupExceptionHandling();

            // Инициализация отладчика
            InitializeDebugger();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        private void SetupExceptionHandling()
        {
            // Обработка исключений UI потока
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                LogException("AppDomain.UnhandledException", exception, isCritical: true);
            };

            // Обработка исключений в задачах
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogException("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            // Обработка исключений в MAUI (для разных платформ)
#if ANDROID
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                LogException("Android.UnhandledExceptionRaiser", args.Exception, isCritical: true);
            };
#elif IOS || MACCATALYST
            ObjCRuntime.Runtime.MarshalManagedException += (sender, args) =>
            {
                LogException("iOS.MarshalManagedException", args.Exception);
            };
#elif WINDOWS
            // Для Windows можно добавить специальные обработчики
            Microsoft.UI.Xaml.Application.Current.UnhandledException += (sender, args) =>
            {
                LogException("Windows.UnhandledException", args.Exception, isCritical: true);
                args.Handled = true; // Предотвращаем краш приложения
            };
#endif

            // Обработка исключений в текущем домене
            Current.Dispatcher.Dispatch(() =>
            {
                // Перехват исключений в основном потоке
            });
        }

        private void InitializeDebugger()
        {
#if DEBUG
            // Включаем подробное логирование в режиме отладки
            Debug.WriteLine("=== DEBUG MODE ENABLED ===");
            Debug.WriteLine($"App started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Debug.WriteLine($"OS: {DeviceInfo.Platform} {DeviceInfo.Version}");
            Debug.WriteLine($"Device: {DeviceInfo.Model}");

            // Тестовый выброс исключения для проверки обработки (можно удалить)
            // Task.Delay(3000).ContinueWith(_ => TestExceptionHandling());
#endif
        }

        private void LogException(string source, Exception exception, bool isCritical = false)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var errorMessage = $"[{timestamp}] [{source}] {(isCritical ? "CRITICAL " : "")}{exception?.GetType().Name}: {exception?.Message}";

                // Логирование в отладчик
                Debug.WriteLine("=== НЕОБРАБОТАННОЕ ИСКЛЮЧЕНИЕ ===");
                Debug.WriteLine(errorMessage);
                Debug.WriteLine($"Stack Trace:\n{exception?.StackTrace}");
                Debug.WriteLine("=== КОНЕЦ ОШИБКИ ===");

                // Логирование в консоль
                Console.WriteLine(errorMessage);

                // Для релизной версии можно добавить запись в файл
                WriteToLogFile(errorMessage, exception);

                // В режиме отладки показываем диалог с ошибкой
#if DEBUG
                if (isCritical || Debugger.IsAttached)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await Current?.MainPage?.DisplayAlert(
                                "Критическая ошибка",
                                $"Источник: {source}\n\nОшибка: {exception?.Message}\n\nПодробности в логах отладчика.",
                                "OK");
                        }
                        catch
                        {
                            // Если даже показ алерта падает
                        }
                    });
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при логировании исключения: {ex}");
            }
        }

        private void WriteToLogFile(string message, Exception exception)
        {
            try
            {
#if !DEBUG
                // В релизе пишем логи в файл
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var logPath = Path.Combine(documentsPath, "TabbedAppDemo_errors.log");
                
                var fullLog = $"{message}\nStack Trace:\n{exception?.StackTrace}\n\n";
                File.AppendAllText(logPath, fullLog);
#endif
            }
            catch
            {
                // Игнорируем ошибки записи в файл
            }
        }

        // Метод для трассировки вызовов (можно использовать для отладки)
        [Conditional("DEBUG")]
        public static void Trace(string message)
        {
            Debug.WriteLine($"[TRACE] {DateTime.Now:HH:mm:ss.fff} - {message}");
        }

        // Метод для тестирования обработки исключений (только в отладке)
        [Conditional("DEBUG")]
        public void TestExceptionHandling()
        {
            Trace("Testing exception handling...");

            // Тест 1: Исключение в фоновой задаче
            Task.Run(() =>
            {
                Trace("Throwing test exception in background task...");
                throw new InvalidOperationException("Тестовое исключение из фоновой задачи");
            });

            // Тест 2: Исключение в UI потоке с задержкой
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(1000);
                Trace("Throwing test exception in UI thread...");
                throw new ArgumentNullException("TestException", "Тестовое исключение из UI потока");
            });
        }

        // Метод для принудительного краша приложения (для тестирования)
        [Conditional("DEBUG")]
        public static void ForceCrash()
        {
            throw new Exception("Принудительный краш приложения для тестирования");
        }
    }
}