using System.ComponentModel.DataAnnotations;

namespace WaveApiMimiker.DTOs;

public class SendMoneyDto
{
    [Required]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}

public class CashInDto
{
    [Required]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string AgentCode { get; set; } = string.Empty;
}

public class CashOutDto
{
    [Required]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string AgentCode { get; set; } = string.Empty;
}

public class AirtimeDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string Operator { get; set; } = string.Empty; // Orange, Free, Moov, MTN, etc.
}

public class FeeRequestDto
{
    [Required]
    public string TransactionType { get; set; } = string.Empty; // Transfer, CashOut, Airtime

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? SenderCountry { get; set; }
    public string? ReceiverCountry { get; set; }
}

public class FeeResponseDto
{
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalDeducted { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string FeeDescription { get; set; } = string.Empty;
}

public class TransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalDeducted { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsInternational { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
