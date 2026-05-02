using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IWalletService
{
    Task<Wallet> CreateWalletAsync(string ownerId, string countryCode);
    WalletDto MapToDto(Wallet wallet);
    Task<(bool Success, string Error, WalletDto? Wallet)> GetWalletAsync(string userId);
    Task ResetDailyLimitIfNeededAsync(Wallet wallet, AppDbContext db);
}

public class WalletService : IWalletService
{
    public async Task<Wallet> CreateWalletAsync(string ownerId, string countryCode)
    {
        var currency = WaveConstants.CountryCurrencies[countryCode.ToUpper()];
        return await Task.FromResult(new Wallet
        {
            OwnerId = ownerId,
            Currency = currency,
            DailyTransferLimit = GetDailyLimit(currency)
        });
    }

    public WalletDto MapToDto(Wallet wallet) => new()
    {
        Id = wallet.Id,
        Balance = wallet.Balance,
        Currency = wallet.Currency,
        DailyTransferLimit = wallet.DailyTransferLimit,
        DailyTransferSent = wallet.DailyTransferSent,
        DailyTransferRemaining = wallet.DailyTransferLimit - wallet.DailyTransferSent
    };

    public async Task<(bool Success, string Error, WalletDto? Wallet)> GetWalletAsync(string userId)
    {
        // Intentionally not injecting AppDbContext here — callers pass it through to keep
        // the service stateless; controllers resolve context from DI per-request.
        return await Task.FromResult<(bool, string, WalletDto?)>((false, "Use overload with db", null));
    }

    public async Task ResetDailyLimitIfNeededAsync(Wallet wallet, AppDbContext db)
    {
        if (DateTime.UtcNow >= wallet.DailyLimitResetAt)
        {
            wallet.DailyTransferSent = 0;
            wallet.DailyLimitResetAt = DateTime.UtcNow.Date.AddDays(1);
            await db.SaveChangesAsync();
        }
    }

    private static decimal GetDailyLimit(string currency) => currency switch
    {
        "UGX" => 10_000_000m,
        "GHS" => 20_000m,
        "GMD" => 75_000m,
        _ => 3_000_000m
    };
}
