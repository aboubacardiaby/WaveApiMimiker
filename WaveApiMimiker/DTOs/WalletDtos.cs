namespace WaveApiMimiker.DTOs;

public class WalletDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal DailyTransferLimit { get; set; }
    public decimal DailyTransferSent { get; set; }
    public decimal DailyTransferRemaining { get; set; }
}
