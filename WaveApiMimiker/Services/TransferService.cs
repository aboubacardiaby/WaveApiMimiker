using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface ITransferService
{
    Task<(bool Success, string Error, TransactionDto? Tx)> SendMoneyAsync(string senderUserId, SendMoneyDto dto);
    Task<(bool Success, string Error, TransactionDto? Tx)> CashInAsync(string agentUserId, CashInDto dto);
    Task<(bool Success, string Error, TransactionDto? Tx)> CashOutAsync(string agentUserId, CashOutDto dto);
    Task<(bool Success, string Error, TransactionDto? Tx)> AirtimeTopUpAsync(string senderUserId, AirtimeDto dto);
    Task<(bool Success, string Error, TransactionDto? Tx)> GetTransactionAsync(string txId, string userId);
    Task<(bool Success, string Error, List<TransactionDto> Transactions)> GetHistoryAsync(string userId, int page, int pageSize);
    Task<FeeResponseDto> CalculateFeeAsync(FeeRequestDto dto, string userCountry, string currency);
}

public class TransferService : ITransferService
{
    private readonly AppDbContext _db;
    private readonly IFeeService _feeService;

    public TransferService(AppDbContext db, IFeeService feeService)
    {
        _db = db;
        _feeService = feeService;
    }

    public async Task<(bool Success, string Error, TransactionDto? Tx)> SendMoneyAsync(string senderUserId, SendMoneyDto dto)
    {
        var sender = await _db.Users.FindAsync(senderUserId);
        if (sender is null) return (false, "Sender not found", null);

        var receiver = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.ReceiverPhone);
        if (receiver is null) return (false, $"No Wave account found for {dto.ReceiverPhone}", null);

        if (sender.PhoneNumber == receiver.PhoneNumber)
            return (false, "Cannot transfer to yourself", null);

        var senderWallet = await _db.Wallets.FirstAsync(w => w.Id == sender.WalletId);
        var receiverWallet = await _db.Wallets.FirstAsync(w => w.Id == receiver.WalletId);

        ResetDailyLimitIfNeeded(senderWallet);

        bool isInternational = sender.CountryCode != receiver.CountryCode;
        var (fee, _) = _feeService.Calculate(TransactionType.Transfer, dto.Amount, sender.CountryCode, receiver.CountryCode);
        var totalDeducted = dto.Amount + fee;

        var validationError = ValidateTransfer(senderWallet, totalDeducted, dto.Amount);
        if (validationError is not null) return (false, validationError, null);

        var tx = new Transaction
        {
            Type = TransactionType.Transfer,
            Status = TransactionStatus.Pending,
            SenderWalletId = senderWallet.Id,
            SenderPhone = sender.PhoneNumber,
            ReceiverWalletId = receiverWallet.Id,
            ReceiverPhone = receiver.PhoneNumber,
            Amount = dto.Amount,
            Fee = fee,
            TotalDeducted = totalDeducted,
            Currency = senderWallet.Currency,
            Note = dto.Note,
            IsInternational = isInternational
        };
        _db.Transactions.Add(tx);

        senderWallet.Balance -= totalDeducted;
        senderWallet.DailyTransferSent += dto.Amount;
        receiverWallet.Balance += dto.Amount;

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, string.Empty, MapToDto(tx));
    }

    public async Task<(bool Success, string Error, TransactionDto? Tx)> CashInAsync(string agentUserId, CashInDto dto)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == agentUserId);
        if (agent is null || !agent.IsActive) return (false, "Agent not found or inactive", null);

        var agentUser = await _db.Users.FindAsync(agentUserId);
        var customer = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.CustomerPhone);
        if (customer is null) return (false, $"No Wave account found for {dto.CustomerPhone}", null);

        var agentWallet = await _db.Wallets.FirstAsync(w => w.Id == agentUser!.WalletId);
        var customerWallet = await _db.Wallets.FirstAsync(w => w.Id == customer.WalletId);

        if (agentWallet.Balance < dto.Amount)
            return (false, "Agent has insufficient float balance", null);

        var tx = new Transaction
        {
            Type = TransactionType.CashIn,
            Status = TransactionStatus.Pending,
            SenderWalletId = agentWallet.Id,
            SenderPhone = agentUser!.PhoneNumber,
            ReceiverWalletId = customerWallet.Id,
            ReceiverPhone = customer.PhoneNumber,
            Amount = dto.Amount,
            Fee = 0,
            TotalDeducted = dto.Amount,
            Currency = customerWallet.Currency,
            AgentId = agent.Id
        };
        _db.Transactions.Add(tx);

        agentWallet.Balance -= dto.Amount;
        customerWallet.Balance += dto.Amount;
        agent.TotalCashInToday += dto.Amount;

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, string.Empty, MapToDto(tx));
    }

    public async Task<(bool Success, string Error, TransactionDto? Tx)> CashOutAsync(string agentUserId, CashOutDto dto)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.UserId == agentUserId);
        if (agent is null || !agent.IsActive) return (false, "Agent not found or inactive", null);

        var agentUser = await _db.Users.FindAsync(agentUserId);
        var customer = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.CustomerPhone);
        if (customer is null) return (false, $"No Wave account found for {dto.CustomerPhone}", null);

        var agentWallet = await _db.Wallets.FirstAsync(w => w.Id == agentUser!.WalletId);
        var customerWallet = await _db.Wallets.FirstAsync(w => w.Id == customer.WalletId);

        var (fee, _) = _feeService.Calculate(TransactionType.CashOut, dto.Amount);
        var totalDeducted = dto.Amount + fee;

        if (customerWallet.Balance < totalDeducted)
            return (false, $"Insufficient balance. Need {totalDeducted} {customerWallet.Currency} (includes {fee} fee)", null);

        var tx = new Transaction
        {
            Type = TransactionType.CashOut,
            Status = TransactionStatus.Pending,
            SenderWalletId = customerWallet.Id,
            SenderPhone = customer.PhoneNumber,
            ReceiverWalletId = agentWallet.Id,
            ReceiverPhone = agentUser!.PhoneNumber,
            Amount = dto.Amount,
            Fee = fee,
            TotalDeducted = totalDeducted,
            Currency = customerWallet.Currency,
            AgentId = agent.Id
        };
        _db.Transactions.Add(tx);

        customerWallet.Balance -= totalDeducted;
        agentWallet.Balance += dto.Amount;
        agent.TotalCashOutToday += dto.Amount;

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, string.Empty, MapToDto(tx));
    }

    public async Task<(bool Success, string Error, TransactionDto? Tx)> AirtimeTopUpAsync(string senderUserId, AirtimeDto dto)
    {
        var sender = await _db.Users.FindAsync(senderUserId);
        if (sender is null) return (false, "User not found", null);

        var wallet = await _db.Wallets.FirstAsync(w => w.Id == sender.WalletId);
        var (fee, _) = _feeService.Calculate(TransactionType.AirtimeTopUp, dto.Amount);
        var totalDeducted = dto.Amount + fee;

        if (wallet.Balance < totalDeducted)
            return (false, $"Insufficient balance. Need {totalDeducted} {wallet.Currency}", null);

        var tx = new Transaction
        {
            Type = TransactionType.AirtimeTopUp,
            Status = TransactionStatus.Pending,
            SenderWalletId = wallet.Id,
            SenderPhone = sender.PhoneNumber,
            ReceiverPhone = dto.PhoneNumber,
            Amount = dto.Amount,
            Fee = fee,
            TotalDeducted = totalDeducted,
            Currency = wallet.Currency,
            Note = $"Airtime top-up via {dto.Operator}"
        };
        _db.Transactions.Add(tx);

        wallet.Balance -= totalDeducted;

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, string.Empty, MapToDto(tx));
    }

    public async Task<(bool Success, string Error, TransactionDto? Tx)> GetTransactionAsync(string txId, string userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, "User not found", null);

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == txId || t.Reference == txId);
        if (tx is null) return (false, "Transaction not found", null);

        if (tx.SenderWalletId != user.WalletId && tx.ReceiverWalletId != user.WalletId)
            return (false, "Transaction not found", null);

        return (true, string.Empty, MapToDto(tx));
    }

    public async Task<(bool Success, string Error, List<TransactionDto> Transactions)> GetHistoryAsync(
        string userId, int page, int pageSize)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, "User not found", new());

        var txs = await _db.Transactions
            .Where(t => t.SenderWalletId == user.WalletId || t.ReceiverWalletId == user.WalletId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToDto(t))
            .ToListAsync();

        return (true, string.Empty, txs);
    }

    public Task<FeeResponseDto> CalculateFeeAsync(FeeRequestDto dto, string userCountry, string currency)
    {
        if (!Enum.TryParse<TransactionType>(dto.TransactionType, true, out var type))
            type = TransactionType.Transfer;

        var (fee, desc) = _feeService.Calculate(type, dto.Amount, userCountry, dto.ReceiverCountry);
        return Task.FromResult(new FeeResponseDto
        {
            Amount = dto.Amount,
            Fee = fee,
            TotalDeducted = dto.Amount + fee,
            Currency = currency,
            FeeDescription = desc
        });
    }

    private static void ResetDailyLimitIfNeeded(Wallet wallet)
    {
        if (DateTime.UtcNow >= wallet.DailyLimitResetAt)
        {
            wallet.DailyTransferSent = 0;
            wallet.DailyLimitResetAt = DateTime.UtcNow.Date.AddDays(1);
        }
    }

    private static string? ValidateTransfer(Wallet wallet, decimal totalDeducted, decimal amount)
    {
        if (wallet.IsLocked) return "Wallet is locked. Contact support.";
        if (wallet.Balance < totalDeducted)
            return $"Insufficient balance. Need {totalDeducted} {wallet.Currency}";
        if (wallet.DailyTransferSent + amount > wallet.DailyTransferLimit)
            return $"Daily transfer limit exceeded. Remaining: {wallet.DailyTransferLimit - wallet.DailyTransferSent} {wallet.Currency}";
        return null;
    }

    private static TransactionDto MapToDto(Transaction tx) => new()
    {
        Id = tx.Id,
        Reference = tx.Reference,
        Type = tx.Type.ToString(),
        Status = tx.Status.ToString(),
        SenderPhone = tx.SenderPhone,
        ReceiverPhone = tx.ReceiverPhone,
        Amount = tx.Amount,
        Fee = tx.Fee,
        TotalDeducted = tx.TotalDeducted,
        Currency = tx.Currency,
        Note = tx.Note,
        IsInternational = tx.IsInternational,
        CreatedAt = tx.CreatedAt,
        CompletedAt = tx.CompletedAt
    };
}
