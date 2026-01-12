using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TabbedAppDemo.Services
{
    public interface ITinkoffApiService
    {
        Task<bool> ConnectAsync(string apiKey);
        Task<AccountInfo> GetAccountInfoAsync();
        Task<PortfolioInfo> GetPortfolioAsync();
        Task<List<Account>> GetAccountsAsync();
        Task<bool> IsConnected();
        void Disconnect();

        // Новые методы для работы с сохранением токена
        Task<bool> TryConnectWithSavedTokenAsync();
        Task SaveTokenAsync(string apiKey);
        Task<bool> HasSavedToken();
        Task ClearSavedToken();

        // Метод для получения операций (основной, без пагинации)
        Task<List<Operation>> GetOperationsAsync(DateTime from, DateTime to, string accountId = null);

        // Новый метод для получения операций с пагинацией
        Task<List<Operation>> GetOperationsWithPaginationAsync(DateTime from, DateTime to,
                                                              string accountId = null,
                                                              int page = 1,
                                                              int pageSize = 100);
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

    // Вспомогательный класс для информации об инструменте
    public class InstrumentInfo
    {
        public string Ticker { get; set; } = "";
        public string Name { get; set; } = "";
        public string InstrumentType { get; set; } = "";
    }
}