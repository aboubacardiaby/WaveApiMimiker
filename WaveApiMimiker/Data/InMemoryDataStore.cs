using WaveApiMimiker.Models;

namespace WaveApiMimiker.Data;

// Thread-safe in-memory store — sufficient for a test/mock API
public class InMemoryDataStore
{
    private readonly Dictionary<string, User> _users = new();
    private readonly Dictionary<string, Wallet> _wallets = new();
    private readonly Dictionary<string, Transaction> _transactions = new();
    private readonly Dictionary<string, Agent> _agents = new();
    private readonly object _lock = new();

    // ── Country → Currency map ──────────────────────────────────────────────
    public static readonly Dictionary<string, string> CountryCurrencies = new()
    {
        ["SN"] = "XOF", // Senegal
        ["CI"] = "XOF", // Côte d'Ivoire
        ["ML"] = "XOF", // Mali
        ["BF"] = "XOF", // Burkina Faso
        ["UG"] = "UGX", // Uganda
        ["GM"] = "GMD", // Gambia
        ["GH"] = "GHS", // Ghana
    };

    public static readonly HashSet<string> SupportedCountries = new(CountryCurrencies.Keys);

    // ── Users ───────────────────────────────────────────────────────────────
    public User? FindUserByPhone(string phone)
    {
        lock (_lock)
            return _users.Values.FirstOrDefault(u => u.PhoneNumber == phone);
    }

    public User? FindUserById(string id)
    {
        lock (_lock)
            return _users.GetValueOrDefault(id);
    }

    public void AddUser(User user)
    {
        lock (_lock) _users[user.Id] = user;
    }

    public void UpdateUser(User user)
    {
        lock (_lock) _users[user.Id] = user;
    }

    // ── Wallets ─────────────────────────────────────────────────────────────
    public Wallet? FindWalletById(string id)
    {
        lock (_lock)
            return _wallets.GetValueOrDefault(id);
    }

    public Wallet? FindWalletByOwnerId(string ownerId)
    {
        lock (_lock)
            return _wallets.Values.FirstOrDefault(w => w.OwnerId == ownerId);
    }

    public void AddWallet(Wallet wallet)
    {
        lock (_lock) _wallets[wallet.Id] = wallet;
    }

    public void UpdateWallet(Wallet wallet)
    {
        lock (_lock)
        {
            wallet.UpdatedAt = DateTime.UtcNow;
            _wallets[wallet.Id] = wallet;
        }
    }

    // ── Transactions ─────────────────────────────────────────────────────────
    public Transaction? FindTransactionById(string id)
    {
        lock (_lock)
            return _transactions.GetValueOrDefault(id);
    }

    public Transaction? FindTransactionByReference(string reference)
    {
        lock (_lock)
            return _transactions.Values.FirstOrDefault(t => t.Reference == reference);
    }

    public List<Transaction> GetTransactionsForWallet(string walletId, int page = 1, int pageSize = 20)
    {
        lock (_lock)
            return _transactions.Values
                .Where(t => t.SenderWalletId == walletId || t.ReceiverWalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
    }

    public void AddTransaction(Transaction transaction)
    {
        lock (_lock) _transactions[transaction.Id] = transaction;
    }

    public void UpdateTransaction(Transaction transaction)
    {
        lock (_lock) _transactions[transaction.Id] = transaction;
    }

    // ── Agents ───────────────────────────────────────────────────────────────
    public Agent? FindAgentByCode(string code)
    {
        lock (_lock)
            return _agents.Values.FirstOrDefault(a => a.AgentCode == code);
    }

    public Agent? FindAgentByUserId(string userId)
    {
        lock (_lock)
            return _agents.Values.FirstOrDefault(a => a.UserId == userId);
    }

    public void AddAgent(Agent agent)
    {
        lock (_lock) _agents[agent.Id] = agent;
    }

    public void UpdateAgent(Agent agent)
    {
        lock (_lock) _agents[agent.Id] = agent;
    }

    public List<Agent> GetAgentsByCountry(string countryCode)
    {
        lock (_lock)
            return _agents.Values.Where(a => a.CountryCode == countryCode && a.IsActive).ToList();
    }
}
