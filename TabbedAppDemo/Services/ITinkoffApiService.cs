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
    }

    public class Account
    {
        public string BrokerAccountType { get; set; }
        public string BrokerAccountId { get; set; }
    }
}