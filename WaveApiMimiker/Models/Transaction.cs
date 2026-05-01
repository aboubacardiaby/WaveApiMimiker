namespace WaveApiMimiker.Models;

public enum TransactionType { Transfer, CashIn, CashOut, AirtimeTopUp, BillPayment }
public enum TransactionStatus { Pending, Completed, Failed, Reversed }

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Reference { get; set; } = GenerateReference();
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public string SenderWalletId { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string ReceiverWalletId { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal Fee { get; set; } = 0;
    public decimal TotalDeducted { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string? Note { get; set; }
    public string? AgentId { get; set; }
    public bool IsInternational { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    private static string GenerateReference()
        => $"WV{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
}
