using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;
using System.Collections.ObjectModel;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab2ViewModel : ObservableObject
    {
        private readonly ITinkoffApiService _tinkoffService;
        private readonly IDialogService _dialogService;
        private readonly IConnectionStateService _connectionState;

        [ObservableProperty]
        private string _title = "💼 Мои Сделки";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isConnected = false;

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

        [ObservableProperty]
        private ObservableCollection<OperationViewModel> _operations = new();

        [ObservableProperty]
        private ObservableCollection<OperationGroupViewModel> _groupedOperations = new();

        [ObservableProperty]
        private string _selectedFilter = "all";

        // Вычисляемые свойства
        public decimal TotalVolume => TotalIncome + Math.Abs(TotalExpense);
        public string NetResultText => NetResult >= 0 ? $"+{NetResult:C}" : $"{NetResult:C}";
        public Color NetResultColor => NetResult >= 0 ? Colors.Green : Colors.Red;
        public string TotalVolumeText => $"{TotalVolume:C}";
        public string PeriodText => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
        public bool HasOperations => Operations.Any();

        public Tab2ViewModel(ITinkoffApiService tinkoffService, IDialogService dialogService, IConnectionStateService connectionState)
        {
            _tinkoffService = tinkoffService;
            _dialogService = dialogService;
            _connectionState = connectionState;
            // Подписываемся на события изменения подключения
            _connectionState.ConnectionChanged += OnConnectionChanged;

            // Устанавливаем начальный статус
            IsConnected = _connectionState.IsConnected;
            // Инициализация при создании
            //InitializeAsync();
        }
        private void OnConnectionChanged(object sender, bool isConnected)
        {
            try
            {
                IsConnected = isConnected;

                if (isConnected)
                {
                    _ = ShowConnectionStatusToast();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnConnectionChanged: {ex.Message}");
            }
        }
        private async void InitializeAsync()
        {
            try
            {
                // Проверяем подключение
                IsConnected = await _tinkoffService.IsConnected();
                if (IsConnected)
                {
                    // Загружаем операции за последнюю неделю
                    await LoadOperationsAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadOperations()
        {
            await LoadOperationsAsync();
        }

        public void UnsubscribeEvents()
        {
            try
            {
                _connectionState.ConnectionChanged -= OnConnectionChanged;
            }
            catch
            {
                // Игнорируем
            }
        }

        // Public метод для вызова из других классов
        public async Task LoadOperationsAsync()
        {
            if (!IsConnected)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    "Сначала подключитесь к Tinkoff API на вкладке 4", "OK");
                return;
            }

            if (StartDate > EndDate)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    "Дата начала не может быть позже даты окончания", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                // Загружаем операции из Tinkoff API
                var apiOperations = await _tinkoffService.GetOperationsAsync(StartDate, EndDate);

                if (apiOperations != null && apiOperations.Any())
                {
                    await ProcessApiOperations(apiOperations);
                    await _dialogService.ShowToastAsync("✅ Сделки загружены", 2);
                }
                else
                {
                    // Если API вернуло пустой результат, используем тестовые данные
                    await LoadTestOperations();
                    await _dialogService.ShowAlertAsync("Информация",
                        "Сделок за выбранный период не найдено", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось загрузить сделки: {ex.Message}\n\nИспользуются тестовые данные.", "OK");

                // Используем тестовые данные при ошибке API
                await LoadTestOperations();
            }
            finally
            {
                IsLoading = false;
            }
        }


        private async Task ProcessApiOperations(List<Operation> apiOperations)
        {
            Operations.Clear();

            foreach (var apiOp in apiOperations)
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

                Operations.Add(operationVm);
            }
        

            UpdateStatistics();
            GroupOperations();
        
        
        }

        private async Task LoadTestOperations()
        {
            // Тестовые данные для демонстрации
            var testOperations = GenerateTestOperations();

            Operations.Clear();
            foreach (var op in testOperations)
            {
                Operations.Add(op);
            }

            UpdateStatistics();
            GroupOperations();
        }

        private List<OperationViewModel> GenerateTestOperations()
        {
            var operations = new List<OperationViewModel>();
            var random = new Random();
            var instruments = new[]
            {
                ("SBER", "Сбербанк", "Акция"),
                ("GAZP", "Газпром", "Акция"),
                ("VTBR", "ВТБ", "Акция"),
                ("YNDX", "Яндекс", "Акция"),
                ("TCSG", "TCS Group", "Акция"),
                ("OFZ", "ОФЗ-26238", "Облигация"),
                ("USD", "Доллар США", "Валюта"),
                ("EUR", "Евро", "Валюта")
            };

            var operationTypes = new[]
            {
                ("Покупка", "OPERATION_TYPE_BUY", Colors.Red),
                ("Продажа", "OPERATION_TYPE_SELL", Colors.Green),
                ("Дивиденды", "OPERATION_TYPE_DIVIDEND", Colors.Green),
                ("Купон", "OPERATION_TYPE_COUPON", Colors.Green),
                ("Комиссия", "OPERATION_TYPE_BROKER_FEE", Colors.Orange),
                ("Пополнение", "OPERATION_TYPE_INPUT", Colors.Blue),
                ("Вывод", "OPERATION_TYPE_OUTPUT", Colors.Blue)
            };

            // Генерируем сделки за период
            var daysDiff = (EndDate - StartDate).Days;
            var numOperations = Math.Min(25, Math.Max(5, daysDiff * 2)); // 5-25 сделок в зависимости от периода

            for (int i = 0; i < numOperations; i++)
            {
                var instrument = instruments[random.Next(instruments.Length)];
                var operationType = operationTypes[random.Next(operationTypes.Length)];
                var date = StartDate.AddDays(random.Next(daysDiff)).AddHours(random.Next(24));
                var quantity = random.Next(1, 100);
                var price = (decimal)(random.NextDouble() * 1000 + 100);
                var amount = quantity * price * (operationType.Item2 == "OPERATION_TYPE_BUY" ? -1 : 1);
                var commission = Math.Abs(amount) * 0.003m;

                operations.Add(new OperationViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = date,
                    Ticker = instrument.Item1,
                    Name = instrument.Item2,
                    InstrumentType = instrument.Item3,
                    OperationType = operationType.Item1,
                    OperationTypeCode = operationType.Item2,
                    Quantity = quantity,
                    Price = price,
                    Amount = amount,
                    Commission = commission,
                    Status = "Исполнена",
                    Icon = GetOperationIcon(operationType.Item2),
                    Color = operationType.Item3,
                    Currency = "RUB"
                });
            }

            // Сортируем по дате (от новых к старым)
            return operations.OrderByDescending(o => o.Date).ToList();
        }

        private string GetOperationIcon(string operationTypeCode)
        {
            return operationTypeCode switch
            {
                "OPERATION_TYPE_BUY" => "📈",
                "OPERATION_TYPE_SELL" => "📉",
                "OPERATION_TYPE_DIVIDEND" => "💰",
                "OPERATION_TYPE_COUPON" => "🎫",
                "OPERATION_TYPE_BROKER_FEE" => "💸",
                "OPERATION_TYPE_INPUT" => "⬆️",
                "OPERATION_TYPE_OUTPUT" => "⬇️",
                _ => "📋"
            };
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
            // В реальном API комиссия приходит отдельно
            // Здесь просто примерная логика
            if (apiOperation.OperationType == "Комиссия" || apiOperation.OperationType == "Брокерская комиссия")
            {
                return Math.Abs(apiOperation.Payment);
            }

            // Для торговых операций - 0.3% комиссии
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
            TotalIncome = Operations.Where(o => o.Amount > 0).Sum(o => o.Amount);
            TotalExpense = Operations.Where(o => o.Amount < 0).Sum(o => o.Amount);
            NetResult = TotalIncome + TotalExpense; // TotalExpense отрицательный
        }

        private void GroupOperations()
        {
            var grouped = Operations
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

            GroupedOperations = new ObservableCollection<OperationGroupViewModel>(grouped);
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
            var incomeCount = Operations.Count(o => o.Amount > 0);
            var expenseCount = Operations.Count(o => o.Amount < 0);
            var buyCount = Operations.Count(o => o.OperationType == "Покупка");
            var sellCount = Operations.Count(o => o.OperationType == "Продажа");

            var mostTraded = Operations
                .GroupBy(o => o.Ticker)
                .Select(g => new { Ticker = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            var stats = $"📊 Статистика сделок:\n\n" +
                       $"📅 Период: {PeriodText}\n" +
                       $"📈 Всего сделок: {Operations.Count}\n" +
                       $"💰 Общий оборот: {TotalVolumeText}\n" +
                       $"📈 Доходы: {TotalIncome:C} ({incomeCount} операций)\n" +
                       $"📉 Расходы: {Math.Abs(TotalExpense):C} ({expenseCount} операций)\n" +
                       $"📊 Итог: {NetResultText}\n" +
                       $"🏷️ Инструментов: {Operations.Select(o => o.Ticker).Distinct().Count()}\n" +
                       $"🔄 Покупок/Продаж: {buyCount}/{sellCount}\n" +
                       $"📈 Самый частый инструмент: {mostTraded?.Ticker ?? "Нет данных"} ({mostTraded?.Count ?? 0} сделок)";

            await _dialogService.ShowAlertAsync("Статистика", stats, "OK");
        }

        [RelayCommand]
        private async Task ApplyFilter()
        {
            // Фильтрация операций
            var filtered = SelectedFilter switch
            {
                "buy" => Operations.Where(o => o.OperationTypeCode == "OPERATION_TYPE_BUY"),
                "sell" => Operations.Where(o => o.OperationTypeCode == "OPERATION_TYPE_SELL"),
                "income" => Operations.Where(o => o.Amount > 0 && !o.OperationTypeCode.Contains("BUY")),
                "expense" => Operations.Where(o => o.Amount < 0 && !o.OperationTypeCode.Contains("SELL")),
                _ => Operations.AsEnumerable()
            };

            var filteredList = filtered.ToList();
            GroupOperationsByList(filteredList);

            await _dialogService.ShowToastAsync($"Фильтр: {GetFilterDescription(SelectedFilter)}", 1);
        }

        private void GroupOperationsByList(List<OperationViewModel> operations)
        {
            var grouped = operations
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

            GroupedOperations = new ObservableCollection<OperationGroupViewModel>(grouped);
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
                    // Можно добавить логику для выбора произвольной даты
                    await _dialogService.ShowAlertAsync("Выбор даты",
                        "Используйте поля даты для выбора произвольного периода", "OK");
                    return;
            }

            await _dialogService.ShowToastAsync($"Период: {GetRangeDescription(range)}", 1);

            // Автоматически загружаем операции для нового периода
            if (IsConnected)
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
            await _dialogService.ShowAlertAsync("Подключение",
                "Перейдите на вкладку 4 для подключения к Tinkoff API", "OK");
        }

        // Метод для обновления подключения (вызывается из Tab4 при успешном подключении)
        
        partial void OnStartDateChanged(DateTime value)
        {
            // Автоматическая загрузка при изменении даты (опционально)
            // if (IsConnected && !IsLoading)
            // {
            //     Task.Run(async () => await LoadOperationsAsync());
            // }
        }

        partial void OnEndDateChanged(DateTime value)
        {
            // Автоматическая загрузка при изменении даты (опционально)
            // if (IsConnected && !IsLoading)
            // {
            //     Task.Run(async () => await LoadOperationsAsync());
            // }
        }
        public async Task ShowConnectionStatusToast()
        {
            await _dialogService.ShowToastAsync("✅ Подключено к Tinkoff API", 2);
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