using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabbedAppDemo.Services
{
    public interface ITinkoffApiService
    {
        // Основные методы подключения
        Task<bool> ConnectAsync(string apiKey);
        void Disconnect();
        Task<bool> IsConnected();

        // Методы для получения данных (для других вкладок)
        Task<AccountInfo> GetAccountInfoAsync();
        Task<PortfolioInfo> GetPortfolioAsync();
        Task<List<Account>> GetAccountsAsync();
        Task<List<Operation>> GetOperationsAsync(DateTime from, DateTime to, string accountId = null);

        // Метод с пагинацией для 2-й вкладки
        Task<List<Operation>> GetOperationsWithPaginationAsync(DateTime from, DateTime to,
                                                              string accountId = null,
                                                              int page = 1,
                                                              int pageSize = 100);

        // Новые методы для работы с токенами по новой логике
        Task<bool> TestConnectionAsync(string apiKey);      // Простая проверка подключения
        Task<string?> LoadTokenFromFile();                  // Только загрузка токена из файла
        Task SaveTokenToFile(string apiKey);                // Только сохранение токена в файл
        Task ClearSavedToken();                             // Удаление сохраненного токена
        Task<bool> HasSavedToken();                         // Проверка наличия сохраненного токена
    }

    public class AccountInfo
    {
        public string BrokerAccountId { get; set; }
        public string BrokerAccountType { get; set; }
        public string Status { get; set; }
        public DateTime OpenedDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public decimal? TotalBalance { get; set; }
        public decimal? ExpectedYield { get; set; }
        public string Currency { get; set; }
        public int TotalAccounts { get; set; }
    }

    public class PortfolioInfo
    {
        public List<PortfolioPosition> Positions { get; set; } = new();
        public decimal TotalPortfolioValue { get; set; }
        public decimal ExpectedYield { get; set; }
        public string Currency { get; set; }
    }

    public class PortfolioPosition
    {
        public string Figi { get; set; }
        public string Ticker { get; set; }
        public string Name { get; set; }
        public string InstrumentType { get; set; }
        public decimal Balance { get; set; }
        public decimal AveragePositionPrice { get; set; }
        public decimal ExpectedYield { get; set; }
        public decimal CurrentPrice { get; set; }
        public string Currency { get; set; } = "RUB";
    }

    public class Account
    {
        public string BrokerAccountType { get; set; }
        public string BrokerAccountId { get; set; }
    }

    public class Operation
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string OperationType { get; set; }
        public string Ticker { get; set; }
        public string Name { get; set; }
        public string InstrumentType { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Payment { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
    }

    public class InstrumentInfo
    {
        public string Ticker { get; set; } = "";
        public string Name { get; set; } = "";
        public string InstrumentType { get; set; } = "";
    }
}