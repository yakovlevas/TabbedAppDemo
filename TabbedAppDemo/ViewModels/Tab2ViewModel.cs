using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab2ViewModel : ObservableObject, IDisposable
    {
        private readonly ITinkoffApiService _tinkoffService;
        private readonly IDialogService _dialogService;
        private readonly IConnectionStateService _connectionState;
        private bool _disposed = false;
        private SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _operationsLock = new object();
        private bool _isInitialized = false;
        private bool _isPageActive = false; // Отслеживаем активность страницы

        [ObservableProperty]
        private string _title = "💼 Мои Сделки";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LoadingPercentage))]
        private double _loadingProgress = 0;

        [ObservableProperty]
        private string _loadingStatus = "";

        [ObservableProperty]
        private bool _hasMoreItems = false;

        [ObservableProperty]
        private int _currentPage = 1;

        private const int PAGE_SIZE = 100;
        private const int CHUNK_SIZE = 50;

        // Приватное поле для хранения состояния
        private bool _isConnected = false; // Явная инициализация

        // Публичное свойство с диагностикой
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    Debug.WriteLine($"[Tab2ViewModel] IsConnected изменен: {_isConnected} -> {value}");
                    SetProperty(ref _isConnected, value);
                }
            }
        }

        [ObservableProperty]
        private DateTime _startDate = DateTime.Now.AddDays(-7);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Now;

        [ObservableProperty]
        private decimal _totalIncome;

        [ObservableProperty]
        private decimal _totalExpense;

        [ObservableProperty]
        private decimal _netResult;

        // Локальный список для быстрого доступа
        private List<OperationViewModel> _operationsList = new();

        // Коллекции для UI
        private ObservableCollection<OperationViewModel> _operations = new();
        public ObservableCollection<OperationViewModel> Operations
        {
            get => _operations;
            private set => SetProperty(ref _operations, value);
        }

        private ObservableCollection<OperationGroupViewModel> _groupedOperations = new();
        public ObservableCollection<OperationGroupViewModel> GroupedOperations
        {
            get => _groupedOperations;
            private set => SetProperty(ref _groupedOperations, value);
        }

        [ObservableProperty]
        private string _selectedFilter = "all";

        // Вычисляемые свойства
        public decimal TotalVolume => TotalIncome + Math.Abs(TotalExpense);
        public string NetResultText => NetResult >= 0 ? $"+{NetResult:C}" : $"{NetResult:C}";
        public Color NetResultColor => NetResult >= 0 ? Colors.Green : Colors.Red;
        public string TotalVolumeText => $"{TotalVolume:C}";
        public string PeriodText => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
        public bool HasOperations => _operationsList.Any();
        public string LoadingPercentage => $"{LoadingProgress * 100:F0}%";

        public Tab2ViewModel(ITinkoffApiService tinkoffService, IDialogService dialogService, IConnectionStateService connectionState)
        {
            _tinkoffService = tinkoffService ?? throw new ArgumentNullException(nameof(tinkoffService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));

            Debug.WriteLine($"[Tab2ViewModel] Конструктор вызван");
            Debug.WriteLine($"[Tab2ViewModel] Начальное состояние подключения из сервиса: {_connectionState.IsConnected}");

            // Инициализируем пустые коллекции для UI
            InitializeEmptyCollections();
        }

        // Метод для вызова при появлении страницы
        public async Task OnAppearing()
        {
            if (_isInitialized)
            {
                _isPageActive = true;
                Debug.WriteLine($"[Tab2ViewModel] OnAppearing: страница снова активна");
                return;
            }

            _isInitialized = true;
            _isPageActive = true;

            Debug.WriteLine($"[Tab2ViewModel] OnAppearing вызван");

            // Обновляем состояние подключения из сервиса
            await UpdateConnectionStateFromService();

            // Подписываемся на изменения
            _connectionState.ConnectionChanged += OnConnectionChanged;
            Debug.WriteLine($"[Tab2ViewModel] Подписка на ConnectionChanged установлена");
        }

        // Метод для вызова при скрытии страницы
        public void OnDisappearing()
        {
            _isPageActive = false;
            Debug.WriteLine($"[Tab2ViewModel] OnDisappearing: страница неактивна");
        }

        private async Task UpdateConnectionStateFromService()
        {
            try
            {
                var currentStatus = _connectionState.IsConnected;
                Debug.WriteLine($"[Tab2ViewModel] Обновление состояния из сервиса: {currentStatus}");

                // Не вызываем MainThread, чтобы избежать рекурсии
                IsConnected = currentStatus;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tab2ViewModel] Ошибка обновления состояния: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private void InitializeEmptyCollections()
        {
            Operations = new ObservableCollection<OperationViewModel>();
            GroupedOperations = new ObservableCollection<OperationGroupViewModel>();
        }

        private void OnConnectionChanged(object sender, bool isConnected)
        {
            Debug.WriteLine($"[Tab2ViewModel] OnConnectionChanged получено событие: {isConnected}");
            Debug.WriteLine($"[Tab2ViewModel] Страница активна: {_isPageActive}");

            // Обновляем состояние
            IsConnected = isConnected;

            // Автоматически загружаем только если страница активна
            if (isConnected && _isPageActive && !_operationsList.Any())
            {
                Debug.WriteLine($"[Tab2ViewModel] Автоматическая загрузка при подключении");
                // Не загружаем сразу, даем пользователю решить
                _ = _dialogService.ShowToastAsync("✅ Подключено. Нажмите 'Загрузить' для получения сделок", 2);
            }
            else if (isConnected)
            {
                _ = _dialogService.ShowToastAsync("✅ Подключено к Tinkoff API", 2);
            }
        }

        [RelayCommand]
        public async Task LoadOperations()
        {
            Debug.WriteLine($"[Tab2ViewModel] LoadOperations команда вызвана");
            Debug.WriteLine($"[Tab2ViewModel] Текущее состояние IsConnected: {IsConnected}");

            await LoadOperationsAsync();
        }

        [RelayCommand]
        public async Task LoadMoreOperations()
        {
            if (!IsConnected || IsLoading || !HasMoreItems)
                return;

            _currentPage++;
            await LoadOperationsPageAsync(_currentPage);
        }

        public async Task LoadOperationsAsync()
        {
            Debug.WriteLine($"[Tab2ViewModel] LoadOperationsAsync начат");
            Debug.WriteLine($"[Tab2ViewModel] Проверка подключения: IsConnected={IsConnected}");

            // Проверяем подключение
            if (!IsConnected)
            {
                Debug.WriteLine($"[Tab2ViewModel] Нет подключения, показываем alert");
                await _dialogService.ShowAlertAsync("Подключение не установлено",
                    "Для загрузки сделок необходимо подключиться к Tinkoff API.\n\n" +
                    "Перейдите на вкладку 4 (Настройки API), чтобы ввести токен и подключиться.", "OK");
                return;
            }

            if (StartDate > EndDate)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    "Дата начала не может быть позже даты окончания", "OK");
                return;
            }

            // Отменяем предыдущую загрузку если она есть
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Добавляем таймаут для всей операции
            var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(45)).Token;
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken);

            // Проверяем, не идет ли уже загрузка
            if (!await _loadSemaphore.WaitAsync(0, linkedTokenSource.Token))
            {
                Debug.WriteLine($"[Tab2ViewModel] Загрузка уже идет, пропускаем");
                await _dialogService.ShowToastAsync("Загрузка уже выполняется", 1);
                return;
            }

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = true;
                    LoadingProgress = 0;
                    LoadingStatus = "Подготовка...";
                });

                Debug.WriteLine($"[Tab2ViewModel] Начинаем загрузку данных...");

                // Очищаем коллекции
                await ClearCollectionsAsync();

                // Загружаем первую страницу
                await LoadOperationsPageAsync(1, linkedTokenSource.Token);

                if (!linkedTokenSource.Token.IsCancellationRequested && _operationsList.Any())
                {
                    await _dialogService.ShowToastAsync($"✅ Загружено {_operationsList.Count} сделок", 2);
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"[Tab2ViewModel] Загрузка отменена (таймаут)");
                await _dialogService.ShowToastAsync("Загрузка отменена по таймауту", 2);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[Tab2ViewModel] Загрузка отменена пользователем");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Tab2ViewModel] Ошибка загрузки: {ex}");
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось загрузить сделки: {ex.Message}", "OK");
                await HandleNoOperations();
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    LoadingProgress = 0;
                    LoadingStatus = "";
                });

                _loadSemaphore.Release();
                Debug.WriteLine($"[Tab2ViewModel] LoadOperationsAsync завершен");
            }
        }

        private async Task LoadOperationsPageAsync(int page, CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.WriteLine($"[Tab2ViewModel] Загрузка страницы {page}...");

                // Обновляем статус
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingStatus = $"Загрузка страницы {page}...";
                });

                // Загружаем операции
                List<Operation> apiOperations = null;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    apiOperations = await _tinkoffService.GetOperationsWithPaginationAsync(
                        StartDate, EndDate,
                        page: page,
                        pageSize: PAGE_SIZE);

                    stopwatch.Stop();
                    Debug.WriteLine($"[Tab2ViewModel] Данные получены за {stopwatch.ElapsedMilliseconds} мс: {apiOperations?.Count ?? 0} операций");
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine($"[Tab2ViewModel] Загрузка отменена");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tab2ViewModel] Ошибка при вызове GetOperationsAsync: {ex.Message}");
                    throw;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"[Tab2ViewModel] Загрузка отменена токеном");
                    return;
                }

                if (apiOperations != null && apiOperations.Any())
                {
                    await ProcessApiOperations(apiOperations, cancellationToken);

                    // Проверяем, есть ли еще данные (упрощенная проверка)
                    HasMoreItems = apiOperations.Count >= PAGE_SIZE;
                }
                else
                {
                    Debug.WriteLine($"[Tab2ViewModel] Нет операций для отображения");
                    if (page == 1) // Только для первой страницы показываем сообщение
                    {
                        await HandleNoOperations();
                        await _dialogService.ShowAlertAsync("Информация",
                            "Сделок за выбранный период не найдено", "OK");
                    }
                    HasMoreItems = false;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"[Tab2ViewModel] Ошибка загрузки страницы {page}: {ex}");
                throw;
            }
        }

        private async Task ProcessApiOperations(List<Operation> apiOperations, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"[Tab2ViewModel] Начало обработки {apiOperations.Count} операций");

            // Выполняем обработку в фоновом потоке
            var operationsToAdd = await Task.Run(async () =>
            {
                var result = new List<OperationViewModel>();
                int processed = 0;

                // Обрабатываем данные частями
                for (int i = 0; i < apiOperations.Count; i += CHUNK_SIZE)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var chunk = apiOperations.Skip(i).Take(CHUNK_SIZE).ToList();

                    foreach (var apiOp in chunk)
                    {
                        try
                        {
                            var operationVm = new OperationViewModel
                            {
                                Id = apiOp.Id,
                                Date = apiOp.Date,
                                Ticker = apiOp.Ticker,
                                Name = apiOp.Name,
                                InstrumentType = apiOp.InstrumentType,
                                OperationType = apiOp.OperationType,
                                OperationTypeCode = GetOperationTypeCode(apiOp.OperationType),
                                Quantity = (int)apiOp.Quantity,
                                Price = apiOp.Price,
                                Amount = apiOp.Payment,
                                Commission = CalculateCommission(apiOp),
                                Status = apiOp.Status,
                                Icon = GetOperationIconByType(apiOp.OperationType),
                                Color = GetOperationColor(apiOp.OperationType, apiOp.Payment),
                                Currency = apiOp.Currency
                            };

                            result.Add(operationVm);
                            processed++;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Tab2ViewModel] ОШИБКА при создании ViewModel: {ex.Message}");
                        }
                    }

                    // Обновляем прогресс
                    var progress = (double)processed / apiOperations.Count;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LoadingProgress = progress;
                        LoadingStatus = $"Обработано {processed} из {apiOperations.Count}";
                    });

                    // Делаем небольшую паузу для отзывчивости UI
                    if (i + CHUNK_SIZE < apiOperations.Count)
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }

                return result;
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"[Tab2ViewModel] Обработка отменена");
                return;
            }

            Debug.WriteLine($"[Tab2ViewModel] Создано {operationsToAdd.Count} ViewModel за {stopwatch.ElapsedMilliseconds} мс");

            // Сохраняем в локальный список
            lock (_operationsLock)
            {
                _operationsList.AddRange(operationsToAdd);
            }

            // Оптимизированное обновление UI коллекции
            await UpdateUiCollectionAsync(operationsToAdd, cancellationToken);

            // Обновляем статистику
            UpdateStatistics();

            // Планируем группировку на потом (отложенная операция)
            if (!cancellationToken.IsCancellationRequested)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await GroupOperations(cancellationToken);
                    }
                }, cancellationToken);
            }

            stopwatch.Stop();
            Debug.WriteLine($"[Tab2ViewModel] Обработка операций завершена за {stopwatch.ElapsedMilliseconds} мс");
        }

        private async Task UpdateUiCollectionAsync(List<OperationViewModel> operationsToAdd, CancellationToken cancellationToken)
        {
            if (operationsToAdd == null || !operationsToAdd.Any())
                return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Operations.Count == 0 && operationsToAdd.Count > 100)
                    {
                        // Для большого количества данных создаем новую коллекцию
                        Operations = new ObservableCollection<OperationViewModel>(operationsToAdd);
                    }
                    else
                    {
                        // Постепенное добавление с задержкой для отзывчивости
                        int addedCount = 0;
                        for (int i = 0; i < operationsToAdd.Count; i += 10)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            var chunk = operationsToAdd.Skip(i).Take(10).ToList();
                            foreach (var item in chunk)
                            {
                                Operations.Add(item);
                                addedCount++;
                            }

                            // Даем UI время на обновление
                            if (i + 10 < operationsToAdd.Count)
                            {
                                await Task.Delay(5, cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tab2ViewModel] ОШИБКА в UpdateUiCollectionAsync: {ex.Message}");
                }
            });
        }

        private async Task ClearCollectionsAsync()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lock (_operationsLock)
                {
                    _operationsList.Clear();
                }
                Operations.Clear();
                GroupedOperations.Clear();

                TotalIncome = 0;
                TotalExpense = 0;
                NetResult = 0;
            });
            await Task.CompletedTask;
        }

        private async Task GroupOperations(CancellationToken cancellationToken = default)
        {
            if (!_operationsList.Any())
            {
                Debug.WriteLine($"[Tab2ViewModel] Нет операций для группировки");
                return;
            }

            Debug.WriteLine($"[Tab2ViewModel] Начало группировки {_operationsList.Count} операций");

            var grouped = await Task.Run(() =>
            {
                try
                {
                    lock (_operationsLock)
                    {
                        return _operationsList
                            .GroupBy(o => o.Date.Date)
                            .Select(g => new OperationGroupViewModel
                            {
                                Date = g.Key,
                                DateText = g.Key.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")),
                                DayTotal = g.Sum(o => o.Amount),
                                Operations = new ObservableCollection<OperationViewModel>(g.OrderByDescending(o => o.Date))
                            })
                            .OrderByDescending(g => g.Date)
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tab2ViewModel] Ошибка при группировке: {ex.Message}");
                    return new List<OperationGroupViewModel>();
                }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            // Обновляем UI в основном потоке
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GroupedOperations = new ObservableCollection<OperationGroupViewModel>(grouped);
                Debug.WriteLine($"[Tab2ViewModel] GroupedOperations обновлено: {GroupedOperations.Count} групп");
            });
        }

        private async Task HandleNoOperations()
        {
            await ClearCollectionsAsync();
        }

        private string GetOperationIconByType(string operationType)
        {
            return operationType switch
            {
                "Покупка" => "📈",
                "Продажа" => "📉",
                "Дивиденды" => "💰",
                "Купон" => "🎫",
                "Комиссия" => "💸",
                "Брокерская комиссия" => "💸",
                "Пополнение" => "⬆️",
                "Вывод" => "⬇️",
                "Налог" => "🏛️",
                _ => "📋"
            };
        }

        private string GetOperationTypeCode(string operationType)
        {
            return operationType switch
            {
                "Покупка" => "OPERATION_TYPE_BUY",
                "Продажа" => "OPERATION_TYPE_SELL",
                "Дивиденды" => "OPERATION_TYPE_DIVIDEND",
                "Купон" => "OPERATION_TYPE_COUPON",
                "Комиссия" => "OPERATION_TYPE_BROKER_FEE",
                "Пополнение" => "OPERATION_TYPE_INPUT",
                "Вывод" => "OPERATION_TYPE_OUTPUT",
                "Налог" => "OPERATION_TYPE_TAX",
                _ => "OPERATION_TYPE_UNKNOWN"
            };
        }

        private decimal CalculateCommission(Operation apiOperation)
        {
            if (apiOperation.OperationType == "Комиссия" || apiOperation.OperationType == "Брокерская комиссия")
            {
                return Math.Abs(apiOperation.Payment);
            }

            if (apiOperation.OperationType == "Покупка" || apiOperation.OperationType == "Продажа")
            {
                return Math.Abs(apiOperation.Payment) * 0.003m;
            }

            return 0;
        }

        private Color GetOperationColor(string operationType, decimal payment)
        {
            if (payment >= 0)
                return Colors.Green;

            return operationType switch
            {
                "Покупка" => Colors.Red,
                "Комиссия" => Colors.Orange,
                "Налог" => Colors.Orange,
                "Вывод" => Colors.Blue,
                _ => Colors.Red
            };
        }

        private void UpdateStatistics()
        {
            lock (_operationsLock)
            {
                var income = _operationsList.Where(o => o.Amount > 0).Sum(o => o.Amount);
                var expense = _operationsList.Where(o => o.Amount < 0).Sum(o => o.Amount);
                var net = income + expense;

                TotalIncome = income;
                TotalExpense = expense;
                NetResult = net;
            }
        }

        [RelayCommand]
        private async Task ShowOperationDetails(OperationViewModel operation)
        {
            if (operation == null) return;

            var details = $"📊 {operation.Name} ({operation.Ticker})\n\n" +
                         $"📅 Дата: {operation.Date:dd.MM.yyyy HH:mm}\n" +
                         $"📋 Тип: {operation.OperationType}\n" +
                         $"📈 Кол-во: {operation.Quantity:N0}\n" +
                         $"💰 Цена: {operation.Price:C}\n" +
                         $"💵 Сумма: {Math.Abs(operation.Amount):C}\n" +
                         $"💸 Комиссия: {operation.Commission:C}\n" +
                         $"📊 Статус: {operation.Status}\n" +
                         $"🏷️ Тип инструмента: {operation.InstrumentType}\n" +
                         $"💱 Валюта: {operation.Currency}";

            await _dialogService.ShowAlertAsync("Детали сделки", details, "OK");
        }

        [RelayCommand]
        private async Task ShowStatistics()
        {
            if (!_operationsList.Any())
            {
                await _dialogService.ShowAlertAsync("Статистика", "Нет данных для отображения", "OK");
                return;
            }

            lock (_operationsLock)
            {
                var incomeCount = _operationsList.Count(o => o.Amount > 0);
                var expenseCount = _operationsList.Count(o => o.Amount < 0);
                var buyCount = _operationsList.Count(o => o.OperationType == "Покупка");
                var sellCount = _operationsList.Count(o => o.OperationType == "Продажа");

                var mostTraded = _operationsList
                    .GroupBy(o => o.Ticker)
                    .Select(g => new { Ticker = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                var stats = $"📊 Статистика сделок:\n\n" +
                           $"📅 Период: {PeriodText}\n" +
                           $"📈 Всего сделок: {_operationsList.Count}\n" +
                           $"💰 Общий оборот: {TotalVolumeText}\n" +
                           $"📈 Доходы: {TotalIncome:C} ({incomeCount} операций)\n" +
                           $"📉 Расходы: {Math.Abs(TotalExpense):C} ({expenseCount} операций)\n" +
                           $"📊 Итог: {NetResultText}\n" +
                           $"🏷️ Инструментов: {_operationsList.Select(o => o.Ticker).Distinct().Count()}\n" +
                           $"🔄 Покупок/Продаж: {buyCount}/{sellCount}\n" +
                           $"📈 Самый частый инструмент: {mostTraded?.Ticker ?? "Нет данных"} ({mostTraded?.Count ?? 0} сделок)";

                _ = _dialogService.ShowAlertAsync("Статистика", stats, "OK");
            }
        }

        [RelayCommand]
        private async Task ApplyFilter()
        {
            if (!_operationsList.Any())
            {
                await _dialogService.ShowToastAsync("Нет данных для фильтрации", 1);
                return;
            }

            IEnumerable<OperationViewModel> filtered;

            lock (_operationsLock)
            {
                switch (SelectedFilter)
                {
                    case "buy":
                        filtered = _operationsList.Where(o => o.OperationTypeCode == "OPERATION_TYPE_BUY");
                        break;
                    case "sell":
                        filtered = _operationsList.Where(o => o.OperationTypeCode == "OPERATION_TYPE_SELL");
                        break;
                    case "income":
                        filtered = _operationsList.Where(o => o.Amount > 0 && !o.OperationTypeCode.Contains("BUY"));
                        break;
                    case "expense":
                        filtered = _operationsList.Where(o => o.Amount < 0 && !o.OperationTypeCode.Contains("SELL"));
                        break;
                    default:
                        filtered = _operationsList;
                        break;
                }
            }

            var filteredList = filtered.ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Operations = new ObservableCollection<OperationViewModel>(filteredList);
            });

            // Обновляем группировку для отфильтрованных данных
            await UpdateFilteredGroups(filteredList);

            await _dialogService.ShowToastAsync($"Фильтр: {GetFilterDescription(SelectedFilter)}", 1);
        }

        private async Task UpdateFilteredGroups(List<OperationViewModel> operations)
        {
            if (!operations.Any())
                return;

            var grouped = await Task.Run(() =>
            {
                return operations
                    .GroupBy(o => o.Date.Date)
                    .Select(g => new OperationGroupViewModel
                    {
                        Date = g.Key,
                        DateText = g.Key.ToString("dd MMMM yyyy"),
                        DayTotal = g.Sum(o => o.Amount),
                        Operations = new ObservableCollection<OperationViewModel>(g.OrderByDescending(o => o.Date))
                    })
                    .OrderByDescending(g => g.Date)
                    .ToList();
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                GroupedOperations = new ObservableCollection<OperationGroupViewModel>(grouped);
            });
        }

        private string GetFilterDescription(string filter)
        {
            return filter switch
            {
                "all" => "Все",
                "buy" => "Покупки",
                "sell" => "Продажи",
                "income" => "Доходы",
                "expense" => "Расходы",
                _ => "Все"
            };
        }

        [RelayCommand]
        private async Task SetDateRange(string range)
        {
            var today = DateTime.Now;

            switch (range)
            {
                case "week":
                    StartDate = today.AddDays(-7);
                    EndDate = today;
                    break;
                case "month":
                    StartDate = today.AddMonths(-1);
                    EndDate = today;
                    break;
                case "quarter":
                    StartDate = today.AddMonths(-3);
                    EndDate = today;
                    break;
                case "year":
                    StartDate = today.AddYears(-1);
                    EndDate = today;
                    break;
                case "custom":
                    await _dialogService.ShowAlertAsync("Выбор даты",
                        "Используйте поля даты для выбора произвольного периода", "OK");
                    return;
            }

            await _dialogService.ShowToastAsync($"Период: {GetRangeDescription(range)}", 1);

            // Автоматически загружаем операции для нового периода
            if (IsConnected && _isPageActive)
            {
                await LoadOperationsAsync();
            }
        }

        private string GetRangeDescription(string range)
        {
            return range switch
            {
                "week" => "Неделя",
                "month" => "Месяц",
                "quarter" => "Квартал",
                "year" => "Год",
                "custom" => "Произвольный",
                _ => "Произвольный"
            };
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadOperationsAsync();
        }

        [RelayCommand]
        private async Task ConnectToTinkoff()
        {
            await _dialogService.ShowAlertAsync("Подключение к Tinkoff API",
                "Для подключения к Tinkoff API перейдите на вкладку 4 (Настройки API)\n\n" +
                "Там вы сможете ввести токен и подключиться к вашему брокерскому счету.", "OK");
        }

        partial void OnStartDateChanged(DateTime value)
        {
            // Автоматическая загрузка при изменении даты - отключено
        }

        partial void OnEndDateChanged(DateTime value)
        {
            // Автоматическая загрузка при изменении даты - отключено
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Debug.WriteLine($"[Tab2ViewModel] Dispose вызван");

                try
                {
                    _connectionState.ConnectionChanged -= OnConnectionChanged;
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _loadSemaphore?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Tab2ViewModel] Ошибка при Dispose: {ex.Message}");
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    public partial class OperationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private DateTime _date;

        [ObservableProperty]
        private string _ticker;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _instrumentType;

        [ObservableProperty]
        private string _operationType;

        [ObservableProperty]
        private string _operationTypeCode;

        [ObservableProperty]
        private int _quantity;

        [ObservableProperty]
        private decimal _price;

        [ObservableProperty]
        private decimal _amount;

        [ObservableProperty]
        private decimal _commission;

        [ObservableProperty]
        private string _status;

        [ObservableProperty]
        private string _icon;

        [ObservableProperty]
        private Color _color;

        [ObservableProperty]
        private string _currency;

        // Вычисляемые свойства
        public string DateText => Date.ToString("dd.MM.yyyy HH:mm");
        public string TimeText => Date.ToString("HH:mm");
        public string AmountText => Amount >= 0 ? $"+{Amount:C}" : $"{Amount:C}";
        public Color AmountColor => Amount >= 0 ? Colors.Green : Colors.Red;
        public string QuantityText => $"{Quantity:N0} шт";
        public string PriceText => $"{Price:C}";
        public string CommissionText => $"{Commission:C}";
        public string FormattedAmount => $"{Math.Abs(Amount):C}";
        public string FormattedDate => Date.ToString("dd MMM yyyy");
    }

    public partial class OperationGroupViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime _date;

        [ObservableProperty]
        private string _dateText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DayTotalText))]
        [NotifyPropertyChangedFor(nameof(DayTotalColor))]
        private decimal _dayTotal;

        [ObservableProperty]
        private ObservableCollection<OperationViewModel> _operations = new();

        public string DayTotalText => DayTotal >= 0 ? $"+{DayTotal:C}" : $"{DayTotal:C}";
        public Color DayTotalColor => DayTotal >= 0 ? Colors.Green : Colors.Red;
    }
}