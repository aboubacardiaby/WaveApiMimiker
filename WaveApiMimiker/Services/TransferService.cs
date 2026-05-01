using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface ITransferService
{
    (bool Success, string Error, TransactionDto? Tx) SendMoney(string senderUserId, SendMoneyDto dto);
    (bool Success, string Error, TransactionDto? Tx) CashIn(string agentUserId, CashInDto dto);
    (bool Success, string Error, TransactionDto? Tx) CashOut(string agentUserId, CashOutDto dto);
    (bool Success, string Error, TransactionDto? Tx) AirtimeTopUp(string senderUserId, AirtimeDto dto);
    (bool Success, string Error, TransactionDto? Tx) GetTransaction(string txId, string userId);
    (bool Success, string Error, List<TransactionDto> Transactions) GetHistory(string userId, int page, int pageSize);
    FeeResponseDto CalculateFee(FeeRequestDto dto, string userCountry, string currency);
}

public class TransferService : ITransferService
{
    private readonly InMemoryDataStore _store;
    private readonly IFeeService _feeService;
    private readonly IWalletService _walletService;

    public TransferService(InMemoryDataStore store, IFeeService feeService, IWalletService walletService)
    {
        _store = store;
        _feeService = feeService;
        _walletService = walletService;
    }

    public (bool Success, string Error, TransactionDto? Tx) SendMoney(string senderUserId, SendMoneyDto dto)
    {
        var sender = _store.FindUserById(senderUserId);
        if (sender is null) return (false, "Sender not found", null);

        var receiver = _store.FindUserByPhone(dto.ReceiverPhone);
        if (receiver is null) return (false, $"No Wave account found for {dto.ReceiverPhone}", null);

        if (sender.PhoneNumber == receiver.PhoneNumber)
            return (false, "Cannot transfer to yourself", null);

        var senderWallet = _store.FindWalletById(sender.WalletId)!;
        var receiverWallet = _store.FindWalletById(receiver.WalletId)!;

        _walletService.ResetDailyLimitIfNeeded(senderWallet);

        bool isInternational = sender.CountryCode != receiver.CountryCode;
        var (fee, feeDesc) = _feeService.Calculate(TransactionType.Transfer, dto.Amount,
            sender.CountryCode, receiver.CountryCode);
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
        _store.AddTransaction(tx);

        senderWallet.Balance -= totalDeducted;
        senderWallet.DailyTransferSent += dto.Amount;
        _store.UpdateWallet(senderWallet);

        // Credit receiver (convert currency if international — simplified 1:1 for test)
        receiverWallet.Balance += dto.Amount;
        _store.UpdateWallet(receiverWallet);

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        _store.UpdateTransaction(tx);

        return (true, string.Empty, MapToDto(tx));
    }

    public (bool Success, string Error, TransactionDto? Tx) CashIn(string agentUserId, CashInDto dto)
    {
        var agent = _store.FindAgentByUserId(agentUserId);
        if (agent is null || !agent.IsActive) return (false, "Agent not found or inactive", null);

        var customer = _store.FindUserByPhone(dto.CustomerPhone);
        if (customer is null) return (false, $"No Wave account found for {dto.CustomerPhone}", null);

        var agentUser = _store.FindUserById(agentUserId)!;
        var agentWallet = _store.FindWalletById(agentUser.WalletId)!;
        var customerWallet = _store.FindWalletById(customer.WalletId)!;

        if (agentWallet.Balance < dto.Amount)
            return (false, "Agent has insufficient float balance", null);

        var tx = new Transaction
        {
            Type = TransactionType.CashIn,
            Status = TransactionStatus.Pending,
            SenderWalletId = agentWallet.Id,
            SenderPhone = agentUser.PhoneNumber,
            ReceiverWalletId = customerWallet.Id,
            ReceiverPhone = customer.PhoneNumber,
            Amount = dto.Amount,
            Fee = 0,
            TotalDeducted = dto.Amount,
            Currency = customerWallet.Currency,
            AgentId = agent.Id
        };
        _store.AddTransaction(tx);

        agentWallet.Balance -= dto.Amount;
        _store.UpdateWallet(agentWallet);

        customerWallet.Balance += dto.Amount;
        _store.UpdateWallet(customerWallet);

        agent.TotalCashInToday += dto.Amount;
        _store.UpdateAgent(agent);

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        _store.UpdateTransaction(tx);

        return (true, string.Empty, MapToDto(tx));
    }

    public (bool Success, string Error, TransactionDto? Tx) CashOut(string agentUserId, CashOutDto dto)
    {
        var agent = _store.FindAgentByUserId(agentUserId);
        if (agent is null || !agent.IsActive) return (false, "Agent not found or inactive", null);

        var customer = _store.FindUserByPhone(dto.CustomerPhone);
        if (customer is null) return (false, $"No Wave account found for {dto.CustomerPhone}", null);

        var agentUser = _store.FindUserById(agentUserId)!;
        var agentWallet = _store.FindWalletById(agentUser.WalletId)!;
        var customerWallet = _store.FindWalletById(customer.WalletId)!;

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
            ReceiverPhone = agentUser.PhoneNumber,
            Amount = dto.Amount,
            Fee = fee,
            TotalDeducted = totalDeducted,
            Currency = customerWallet.Currency,
            AgentId = agent.Id
        };
        _store.AddTransaction(tx);

        customerWallet.Balance -= totalDeducted;
        _store.UpdateWallet(customerWallet);

        agentWallet.Balance += dto.Amount; // agent receives cash amount, fee goes to Wave
        _store.UpdateWallet(agentWallet);

        agent.TotalCashOutToday += dto.Amount;
        _store.UpdateAgent(agent);

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        _store.UpdateTransaction(tx);

        return (true, string.Empty, MapToDto(tx));
    }

    public (bool Success, string Error, TransactionDto? Tx) AirtimeTopUp(string senderUserId, AirtimeDto dto)
    {
        var sender = _store.FindUserById(senderUserId);
        if (sender is null) return (false, "User not found", null);

        var wallet = _store.FindWalletById(sender.WalletId)!;
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
        _store.AddTransaction(tx);

        wallet.Balance -= totalDeducted;
        _store.UpdateWallet(wallet);

        tx.Status = TransactionStatus.Completed;
        tx.CompletedAt = DateTime.UtcNow;
        _store.UpdateTransaction(tx);

        return (true, string.Empty, MapToDto(tx));
    }

    public (bool Success, string Error, TransactionDto? Tx) GetTransaction(string txId, string userId)
    {
        var user = _store.FindUserById(userId);
        if (user is null) return (false, "User not found", null);

        var tx = _store.FindTransactionById(txId)
            ?? _store.FindTransactionByReference(txId);

        if (tx is null) return (false, "Transaction not found", null);

        // Users may only see their own transactions
        var wallet = _store.FindWalletById(user.WalletId)!;
        if (tx.SenderWalletId != wallet.Id && tx.ReceiverWalletId != wallet.Id)
            return (false, "Transaction not found", null);

        return (true, string.Empty, MapToDto(tx));
    }

    public (bool Success, string Error, List<TransactionDto> Transactions) GetHistory(
        string userId, int page, int pageSize)
    {
        var user = _store.FindUserById(userId);
        if (user is null) return (false, "User not found", new());

        var txs = _store.GetTransactionsForWallet(user.WalletId, page, pageSize)
            .Select(MapToDto)
            .ToList();

        return (true, string.Empty, txs);
    }

    public FeeResponseDto CalculateFee(FeeRequestDto dto, string userCountry, string currency)
    {
        if (!Enum.TryParse<TransactionType>(dto.TransactionType, true, out var type))
            type = TransactionType.Transfer;

        var (fee, desc) = _feeService.Calculate(type, dto.Amount, userCountry, dto.ReceiverCountry);
        return new FeeResponseDto
        {
            Amount = dto.Amount,
            Fee = fee,
            TotalDeducted = dto.Amount + fee,
            Currency = currency,
            FeeDescription = desc
        };
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
