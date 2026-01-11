// Должно быть ТОЛЬКО это:
public interface ITinkoffApiService
{
    Task<bool> ConnectAsync(string apiKey);
    Task<AccountInfo> GetAccountInfoAsync();
    Task<bool> IsConnected();
}

public class AccountInfo
{
    public string BrokerAccountId { get; set; }
    public string Status { get; set; }
    public DateTime LastUpdate { get; set; }
    public decimal? TotalBalance { get; set; }
    public int TotalAccounts { get; set; }
}