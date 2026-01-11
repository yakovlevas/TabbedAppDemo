using System.Text.Json.Serialization;

namespace TabbedAppDemo.Services
{
    // Модели для парсинга реальных ответов Tinkoff API

    public class TinkoffApiResponse<T>
    {
        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("payload")]
        public T Payload { get; set; }
    }

    public class AccountsPayload
    {
        [JsonPropertyName("accounts")]
        public List<TinkoffAccount> Accounts { get; set; }
    }

    public class TinkoffAccount
    {
        [JsonPropertyName("brokerAccountType")]
        public string BrokerAccountType { get; set; }

        [JsonPropertyName("brokerAccountId")]
        public string BrokerAccountId { get; set; }
    }

    public class PortfolioPayload
    {
        [JsonPropertyName("positions")]
        public List<PortfolioPosition> Positions { get; set; }

        [JsonPropertyName("totalAmountCurrencies")]
        public CurrencyAmount TotalAmountCurrencies { get; set; }

        [JsonPropertyName("totalAmountBonds")]
        public CurrencyAmount TotalAmountBonds { get; set; }

        [JsonPropertyName("totalAmountEtf")]
        public CurrencyAmount TotalAmountEtf { get; set; }

        [JsonPropertyName("totalAmountFutures")]
        public CurrencyAmount TotalAmountFutures { get; set; }

        [JsonPropertyName("expectedYield")]
        public CurrencyAmount ExpectedYield { get; set; }
    }

    public class PortfolioPosition
    {
        [JsonPropertyName("figi")]
        public string Figi { get; set; }

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("averagePositionPrice")]
        public MoneyAmount AveragePositionPrice { get; set; }

        [JsonPropertyName("expectedYield")]
        public MoneyAmount ExpectedYield { get; set; }
    }

    public class CurrencyAmount
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }

    public class MoneyAmount
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }
}