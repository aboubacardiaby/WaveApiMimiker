using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IWalletService
{
    Wallet CreateWallet(string ownerId, string countryCode);
    WalletDto MapToDto(Wallet wallet);
    (bool Success, string Error, WalletDto? Wallet) GetWallet(string userId);
    (bool Success, string Error) ResetDailyLimitIfNeeded(Wallet wallet);
}

public class WalletService : IWalletService
{
    private readonly InMemoryDataStore _store;

    public WalletService(InMemoryDataStore store)
    {
        _store = store;
    }

    public Wallet CreateWallet(string ownerId, string countryCode)
    {
        var currency = InMemoryDataStore.CountryCurrencies[countryCode.ToUpper()];
        var wallet = new Wallet
        {
            OwnerId = ownerId,
            Currency = currency,
            DailyTransferLimit = GetDailyLimit(currency),
        };
        _store.AddWallet(wallet);
        return wallet;
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

    public (bool Success, string Error, WalletDto? Wallet) GetWallet(string userId)
    {
        var wallet = _store.FindWalletByOwnerId(userId);
        if (wallet is null)
            return (false, "Wallet not found", null);

        ResetDailyLimitIfNeeded(wallet);
        return (true, string.Empty, MapToDto(wallet));
    }

    public (bool Success, string Error) ResetDailyLimitIfNeeded(Wallet wallet)
    {
        if (DateTime.UtcNow >= wallet.DailyLimitResetAt)
        {
            wallet.DailyTransferSent = 0;
            wallet.DailyLimitResetAt = DateTime.UtcNow.Date.AddDays(1);
            _store.UpdateWallet(wallet);
        }
        return (true, string.Empty);
    }

    private static decimal GetDailyLimit(string currency) => currency switch
    {
        "UGX" => 10_000_000m,
        "GHS" => 20_000m,
        "GMD" => 75_000m,
        _ => 3_000_000m   // XOF
    };
}
