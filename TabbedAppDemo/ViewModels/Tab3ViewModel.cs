using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TabbedAppDemo.Services;
using System.Collections.ObjectModel;

namespace TabbedAppDemo.ViewModels
{
    public partial class Tab3ViewModel : ObservableObject
    {
        private readonly ITinkoffApiService _tinkoffService;
        private readonly IDialogService _dialogService;
        private bool _isPageActive = false;

        [ObservableProperty]
        private string _title = "📊 Мой Портфель";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionStatusText))]
        [NotifyPropertyChangedFor(nameof(ConnectionStatusColor))]
        private bool _isConnected = false; // Явная инициализация

        [ObservableProperty]
        private decimal _totalPortfolioValue;

        [ObservableProperty]
        private decimal _totalProfitLoss;

        [ObservableProperty]
        private decimal _dailyChange;

        [ObservableProperty]
        private decimal _dailyChangePercent;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalChangeColor))]
        [NotifyPropertyChangedFor(nameof(TotalChangeText))]
        private decimal _totalChangePercent;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPortfolioEmpty))]
        private ObservableCollection<PortfolioItemViewModel> _portfolioItems = new();

        // Вычисляемые свойства
        public Color TotalChangeColor => TotalChangePercent >= 0 ? Colors.Green : Colors.Red;
        public string TotalChangeText => TotalChangePercent >= 0 ? $"+{TotalChangePercent:F2}%" : $"{TotalChangePercent:F2}%";
        public string DailyChangeText => DailyChange >= 0 ? $"+{DailyChange:C}" : $"{DailyChange:C}";
        public Color DailyChangeColor => DailyChange >= 0 ? Colors.Green : Colors.Red;
        public string ConnectionStatusText => IsConnected ? "Подключено к Tinkoff" : "Не подключено";
        public Color ConnectionStatusColor => IsConnected ? Colors.Green : Colors.Orange;
        public bool IsPortfolioEmpty => !PortfolioItems.Any();

        public Tab3ViewModel(ITinkoffApiService tinkoffService, IDialogService dialogService)
        {
            _tinkoffService = tinkoffService;
            _dialogService = dialogService;

            // НЕ загружаем портфель при создании ViewModel!
            // Пусть пользователь сделает это явно
            //Debug.WriteLine("[Tab3ViewModel] Конструктор вызван");
        }

        // Метод для вызова при появлении страницы
        public void OnAppearing()
        {
            _isPageActive = true;
           // Debug.WriteLine("[Tab3ViewModel] OnAppearing: страница активна");
        }

        // Метод для вызова при скрытии страницы
        public void OnDisappearing()
        {
            _isPageActive = false;
           // Debug.WriteLine("[Tab3ViewModel] OnDisappearing: страница неактивна");
        }

        public async Task CheckConnectionAndLoadPortfolioAsync()
        {
            try
            {
                //Debug.WriteLine("[Tab3ViewModel] Проверка подключения...");

                // Проверяем подключение через сервис
                IsConnected = await _tinkoffService.IsConnected();
                //Debug.WriteLine($"[Tab3ViewModel] Состояние подключения: {IsConnected}");

                if (IsConnected && _isPageActive)
                {
                    await LoadPortfolio();
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"[Tab3ViewModel] Ошибка проверки подключения: {ex.Message}");
                IsConnected = false;
            }
        }

        [RelayCommand]
        private async Task LoadPortfolio()
        {
            if (!_isPageActive)
            {
                //Debug.WriteLine("[Tab3ViewModel] Страница неактивна, пропускаем загрузку");
                return;
            }

            // Проверяем подключение перед загрузкой
            if (!await CheckConnectionAsync())
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    "Сначала подключитесь к Tinkoff API на вкладке 4", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var portfolio = await _tinkoffService.GetPortfolioAsync();

                if (portfolio != null && portfolio.Positions.Any())
                {
                    UpdatePortfolioData(portfolio);
                    await _dialogService.ShowToastAsync("✅ Портфель обновлен", 2);
                }
                else
                {
                    await _dialogService.ShowAlertAsync("Информация",
                        "Портфель пуст или не удалось загрузить данные", "OK");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Ошибка",
                    $"Не удалось загрузить портфель: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<bool> CheckConnectionAsync()
        {
            try
            {
                return await _tinkoffService.IsConnected();
            }
            catch
            {
                return false;
            }
        }

        [RelayCommand]
        private async Task RefreshPortfolio()
        {
            await LoadPortfolio();
        }

        [RelayCommand]
        private async Task ShowPositionDetails(PortfolioItemViewModel item)
        {
            if (item == null) return;

            var details = $"📊 {item.Name} ({item.Ticker})\n\n" +
                         $"💰 Стоимость позиции: {item.TotalValue:C}\n" +
                         $"📈 Кол-во бумаг: {item.Quantity:N2}\n" +
                         $"🏷️ Средняя цена: {item.AveragePrice:C}\n" +
                         $"📊 Текущая цена: {item.CurrentPrice:C}\n" +
                         $"📊 Изменение цены: {item.PriceChange:C}\n" +
                         $"📈 % изменения: {item.ChangePercent:F2}%\n" +
                         $"📊 Изменение за день: {item.DailyChangeText}\n" +
                         $"💸 Тип: {item.InstrumentType}";

            await _dialogService.ShowAlertAsync("Детали позиции", details, "OK");
        }

        [RelayCommand]
        private async Task ShowPortfolioSummary()
        {
            var summary = $"📊 Сводка портфеля:\n\n" +
                         $"💰 Общая стоимость: {TotalPortfolioValue:C}\n" +
                         $"📈 Общая доходность: {TotalChangeText}\n" +
                         $"📊 Изменение за день: {DailyChangeText}\n" +
                         $"📈 Позиций в портфеле: {PortfolioItems.Count}\n" +
                         $"📋 Инструментов: {PortfolioItems.Select(p => p.InstrumentType).Distinct().Count()}\n" +
                         $"💵 Самая крупная позиция: {PortfolioItems.FirstOrDefault()?.Ticker ?? "Нет данных"}\n" +
                         $"🔄 Последнее обновление: {DateTime.Now:HH:mm:ss}";

            await _dialogService.ShowAlertAsync("Сводка портфеля", summary, "OK");
        }

        [RelayCommand]
        private async Task ConnectToTinkoff()
        {
            await _dialogService.ShowAlertAsync("Подключение",
                "Перейдите на вкладку 4 для подключения к Tinkoff API", "OK");
        }

        [RelayCommand]
        private async Task SortBy(string sortOption)
        {
            if (IsPortfolioEmpty) return;

            var sortedItems = sortOption switch
            {
                "value" => PortfolioItems.OrderByDescending(p => p.TotalValue),
                "change" => PortfolioItems.OrderByDescending(p => p.ChangePercent),
                "name" => PortfolioItems.OrderBy(p => p.Ticker),
                "type" => PortfolioItems.OrderBy(p => p.InstrumentType),
                _ => PortfolioItems.OrderByDescending(p => p.TotalValue)
            };

            PortfolioItems = new ObservableCollection<PortfolioItemViewModel>(sortedItems);
            await _dialogService.ShowToastAsync($"Сортировка по: {GetSortDescription(sortOption)}", 1);
        }

        private string GetSortDescription(string sortOption)
        {
            return sortOption switch
            {
                "value" => "стоимости",
                "change" => "доходности",
                "name" => "названию",
                "type" => "типу",
                _ => "стоимости"
            };
        }

        private void UpdatePortfolioData(Services.PortfolioInfo portfolio)
        {
            TotalPortfolioValue = portfolio.TotalPortfolioValue;

            // Рассчитываем изменения
            var random = new Random();
            TotalProfitLoss = TotalPortfolioValue * (decimal)(random.NextDouble() * 0.1 - 0.05);
            TotalChangePercent = TotalPortfolioValue > 0 ? (TotalProfitLoss / TotalPortfolioValue) * 100 : 0;

            DailyChange = TotalPortfolioValue * (decimal)(random.NextDouble() * 0.03 - 0.015);
            DailyChangePercent = TotalPortfolioValue > 0 ? (DailyChange / TotalPortfolioValue) * 100 : 0;

            // Обновляем список позиций
            PortfolioItems.Clear();

            foreach (var position in portfolio.Positions)
            {
                var positionValue = position.Balance * position.CurrentPrice;
                var priceChange = position.CurrentPrice - position.AveragePositionPrice;
                var changePercent = position.AveragePositionPrice > 0
                    ? (priceChange / position.AveragePositionPrice) * 100
                    : 0;
                var dailyChange = position.CurrentPrice * (decimal)(random.NextDouble() * 0.02 - 0.01);

                PortfolioItems.Add(new PortfolioItemViewModel
                {
                    Ticker = !string.IsNullOrEmpty(position.Ticker) ? position.Ticker : position.Figi,
                    Name = !string.IsNullOrEmpty(position.Name) ? position.Name : "Неизвестный инструмент",
                    InstrumentType = GetInstrumentTypeName(position.InstrumentType),
                    Quantity = position.Balance,
                    AveragePrice = position.AveragePositionPrice,
                    CurrentPrice = position.CurrentPrice,
                    PriceChange = priceChange,
                    ChangePercent = changePercent,
                    DailyChange = dailyChange,
                    TotalValue = positionValue,
                    Icon = GetInstrumentIcon(position.InstrumentType),
                    Currency = "RUB"
                });
            }

            // Сортируем по стоимости позиции
            PortfolioItems = new ObservableCollection<PortfolioItemViewModel>(
                PortfolioItems.OrderByDescending(p => p.TotalValue));
        }

        private string GetInstrumentTypeName(string type)
        {
            return type?.ToLower() switch
            {
                "share" or "stock" => "Акция",
                "bond" => "Облигация",
                "etf" => "Фонд",
                "currency" => "Валюта",
                "future" => "Фьючерс",
                "option" => "Опцион",
                _ => type ?? "Инструмент"
            };
        }

        private string GetInstrumentIcon(string type)
        {
            return type?.ToLower() switch
            {
                "share" or "stock" => "📈",
                "bond" => "📊",
                "etf" => "🏦",
                "currency" => "💵",
                "future" => "⚡",
                "option" => "📜",
                _ => "📋"
            };
        }

        // Метод для обновления подключения
        public async Task OnTinkoffConnected()
        {
            IsConnected = true;
            if (_isPageActive)
            {
                await LoadPortfolio();
            }
        }

        // Метод для обновления при отключении
        public void OnTinkoffDisconnected()
        {
            IsConnected = false;
            PortfolioItems.Clear();
            TotalPortfolioValue = 0;
            TotalChangePercent = 0;
            DailyChange = 0;
        }
    }

    public partial class PortfolioItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _ticker;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _instrumentType;

        [ObservableProperty]
        private string _icon;

        [ObservableProperty]
        private string _currency;

        [ObservableProperty]
        private decimal _quantity;

        [ObservableProperty]
        private decimal _averagePrice;

        [ObservableProperty]
        private decimal _currentPrice;

        [ObservableProperty]
        private decimal _priceChange;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ChangeColor))]
        [NotifyPropertyChangedFor(nameof(ChangeText))]
        private decimal _changePercent;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DailyChangeText))]
        [NotifyPropertyChangedFor(nameof(DailyChangeColor))]
        private decimal _dailyChange;

        [ObservableProperty]
        private decimal _totalValue;

        // Вычисляемые свойства для XAML
        public Color ChangeColor => ChangePercent >= 0 ? Colors.Green : Colors.Red;
        public string ChangeText => ChangePercent >= 0 ? $"+{ChangePercent:F2}%" : $"{ChangePercent:F2}%";
        public string DailyChangeText => DailyChange >= 0 ? $"+{DailyChange:C}" : $"{DailyChange:C}";
        public Color DailyChangeColor => DailyChange >= 0 ? Colors.Green : Colors.Red;
        public string PriceChangeText => PriceChange >= 0 ? $"+{PriceChange:C}" : $"{PriceChange:C}";
        public Color PriceChangeColor => PriceChange >= 0 ? Colors.Green : Colors.Red;
        public string FormattedQuantity => $"{Quantity:N2} шт";
        public string FormattedTotalValue => $"{TotalValue:C}";
        public string FormattedCurrentPrice => $"{CurrentPrice:C}";
    }
}