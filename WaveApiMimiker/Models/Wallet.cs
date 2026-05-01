namespace WaveApiMimiker.Models;

public class Wallet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OwnerId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty; // XOF, UGX, GMD, GHS
    public decimal Balance { get; set; } = 0;
    public decimal DailyTransferLimit { get; set; } = 1_000_000; // in local currency
    public decimal DailyTransferSent { get; set; } = 0;
    public DateTime DailyLimitResetAt { get; set; } = DateTime.UtcNow.Date.AddDays(1);
    public bool IsLocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
