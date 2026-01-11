namespace TabbedAppDemo.Services
{
    public static class TinkoffApiEndpoints
    {
        // Базовый URL API
        public const string BaseUrl = "https://api-invest.tinkoff.ru/openapi/";
        public const string SandboxBaseUrl = "https://api-invest.tinkoff.ru/openapi/sandbox/";

        // Основные endpoints
        public const string UserAccounts = "user/accounts";
        public const string Portfolio = "portfolio";
        public const string PortfolioCurrencies = "portfolio/currencies";
        public const string MarketStocks = "market/stocks";
        public const string MarketBonds = "market/bonds";
        public const string MarketEtfs = "market/etfs";
        public const string MarketCurrencies = "market/currencies";
        public const string MarketOrderbook = "market/orderbook";
        public const string MarketCandles = "market/candles";
        public const string Operations = "operations";
        public const string UserLimits = "user/limits";

        // Sandbox endpoints (для тестирования)
        public const string SandboxRegister = "sandbox/register";
        public const string SandboxClear = "sandbox/clear";
        public const string SandboxCurrenciesBalance = "sandbox/currencies/balance";
        public const string SandboxPositionsBalance = "sandbox/positions/balance";

        // Orders
        public const string Orders = "orders";
        public const string OrdersLimitOrder = "orders/limit-order";
        public const string OrdersMarketOrder = "orders/market-order";
    }
}