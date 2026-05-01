using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IFeeService
{
    (decimal Fee, string Description) Calculate(TransactionType type, decimal amount,
        string? senderCountry = null, string? receiverCountry = null);
}

public class FeeService : IFeeService
{
    // Wave fee schedule (approximate real-world values)
    private const decimal CashOutRate = 0.01m;          // 1 % for cash-out
    private const decimal IntlTransferRate = 0.015m;    // 1.5 % for cross-country
    private const decimal AirtimeRate = 0.02m;          // 2 % airtime convenience fee

    public (decimal Fee, string Description) Calculate(TransactionType type, decimal amount,
        string? senderCountry = null, string? receiverCountry = null)
    {
        return type switch
        {
            TransactionType.Transfer => CalculateTransferFee(amount, senderCountry, receiverCountry),
            TransactionType.CashIn => (0, "Cash-in is free"),
            TransactionType.CashOut => (Round(amount * CashOutRate), $"1% cash-out fee"),
            TransactionType.AirtimeTopUp => (Round(amount * AirtimeRate), "2% airtime convenience fee"),
            TransactionType.BillPayment => (0, "Bill payment is free"),
            _ => (0, "No fee")
        };
    }

    private (decimal Fee, string Description) CalculateTransferFee(decimal amount,
        string? senderCountry, string? receiverCountry)
    {
        if (!string.IsNullOrEmpty(senderCountry) &&
            !string.IsNullOrEmpty(receiverCountry) &&
            senderCountry != receiverCountry)
        {
            return (Round(amount * IntlTransferRate), "1.5% international transfer fee");
        }
        return (0, "Domestic transfer is free");
    }

    private static decimal Round(decimal value) => Math.Round(value, 2);
}
