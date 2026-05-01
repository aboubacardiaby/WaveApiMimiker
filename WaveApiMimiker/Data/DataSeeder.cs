using WaveApiMimiker.Models;

namespace WaveApiMimiker.Data;

public static class DataSeeder
{
    public static void Seed(InMemoryDataStore store)
    {
        var users = new[]
        {
            // ── Senegal (XOF) ──────────────────────────────────────────────
            new SeedUser("+221771000001", "Amadou Diallo",     "SN", "1234", 250_000m,  UserRole.Customer),
            new SeedUser("+221772000002", "Fatou Ndiaye",      "SN", "1234", 80_000m,   UserRole.Customer),
            new SeedUser("+221773000003", "Moussa Sow",        "SN", "1234", 500_000m,  UserRole.Agent,
                AgentBusiness: "Chez Moussa Mobile Money", AgentCity: "Dakar"),
            new SeedUser("+221774000004", "Aïssatou Ba",       "SN", "1234", 45_000m,   UserRole.Customer),
            new SeedUser("+221775000005", "Ibrahima Fall",     "SN", "1234", 1_200_000m,UserRole.Customer),
            new SeedUser("+221778689865", "Ousmane Diaby",     "SN", "1234", 100_000m,  UserRole.Customer),
            new SeedUser("+221773005100", "Aliou Mbaye",       "SN", "1234", 100_000m,  UserRole.Customer),
            new SeedUser("+2207878788",   "Cheikh Sarr",       "SN", "1234", 100_000m,  UserRole.Customer),

            // ── Côte d'Ivoire (XOF) ───────────────────────────────────────
            new SeedUser("+2250701000001", "Koffi Assi",       "CI", "1234", 300_000m,  UserRole.Customer),
            new SeedUser("+2250702000002", "Aya Touré",        "CI", "1234", 150_000m,  UserRole.Customer),
            new SeedUser("+2250703000003", "Mamadou Koné",     "CI", "1234", 700_000m,  UserRole.Agent,
                AgentBusiness: "Wave Point Cocody", AgentCity: "Abidjan"),

            // ── Mali (XOF) ────────────────────────────────────────────────
            new SeedUser("+22370000001",  "Seydou Keïta",     "ML", "1234", 175_000m,  UserRole.Customer),
            new SeedUser("+22371000002",  "Mariam Coulibaly", "ML", "1234", 90_000m,   UserRole.Customer),
            new SeedUser("+22372000003",  "Boubacar Traoré",  "ML", "1234", 400_000m,  UserRole.Agent,
                AgentBusiness: "Transfert Rapide Bamako", AgentCity: "Bamako"),

            // ── Burkina Faso (XOF) ────────────────────────────────────────
            new SeedUser("+22670000001",  "Rasmané Ouédraogo","BF", "1234", 60_000m,   UserRole.Customer),
            new SeedUser("+22671000002",  "Adja Sawadogo",    "BF", "1234", 220_000m,  UserRole.Customer),

            // ── Uganda (UGX) ──────────────────────────────────────────────
            new SeedUser("+256701000001", "Moses Okonkwo",    "UG", "1234", 800_000m,  UserRole.Customer),
            new SeedUser("+256702000002", "Grace Nakato",     "UG", "1234", 250_000m,  UserRole.Customer),
            new SeedUser("+256703000003", "John Ssekibuule",  "UG", "1234", 2_000_000m,UserRole.Agent,
                AgentBusiness: "Kampala Wave Agency", AgentCity: "Kampala"),

            // ── Gambia (GMD) ──────────────────────────────────────────────
            new SeedUser("+2203000001",   "Lamin Jallow",     "GM", "1234", 12_000m,   UserRole.Customer),
            new SeedUser("+2203000002",   "Sainabou Ceesay",  "GM", "1234", 5_500m,    UserRole.Customer),
            new SeedUser("+220000009",    "Binta Jammeh",     "GM", "1234", 8_000m,    UserRole.Customer),

            // ── Ghana (GHS) ───────────────────────────────────────────────
            new SeedUser("+233501000001", "Kwame Mensah",     "GH", "1234", 3_500m,    UserRole.Customer),
            new SeedUser("+233502000002", "Akua Boateng",     "GH", "1234", 1_200m,    UserRole.Customer),
            new SeedUser("+233503000003", "Kofi Agyemang",    "GH", "1234", 8_000m,    UserRole.Agent,
                AgentBusiness: "Accra Wave Hub", AgentCity: "Accra"),
        };

        foreach (var seed in users)
        {
            var user = new User
            {
                PhoneNumber = seed.Phone,
                FullName    = seed.Name,
                CountryCode = seed.Country,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seed.Pin),
                Role   = seed.Role,
                Status = AccountStatus.Active
            };

            var currency = InMemoryDataStore.CountryCurrencies[seed.Country];
            var wallet = new Wallet
            {
                OwnerId  = user.Id,
                Currency = currency,
                Balance  = seed.Balance,
                DailyTransferLimit = GetDailyLimit(currency)
            };

            user.WalletId = wallet.Id;
            store.AddUser(user);
            store.AddWallet(wallet);

            if (seed.Role == UserRole.Agent && seed.AgentBusiness is not null)
            {
                var agent = new Agent
                {
                    UserId       = user.Id,
                    AgentCode    = $"{seed.Country}-{seed.Phone[^5..]}",
                    BusinessName = seed.AgentBusiness,
                    CountryCode  = seed.Country,
                    PhoneNumber  = seed.Phone,
                    City         = seed.AgentCity ?? seed.Country,
                    IsActive     = true
                };
                store.AddAgent(agent);
            }
        }

        // Seed a few historical transactions so /history isn't empty
        SeedTransactions(store);
    }

    private static void SeedTransactions(InMemoryDataStore store)
    {
        var amadou  = store.FindUserByPhone("+221771000001");
        var fatou   = store.FindUserByPhone("+221772000002");
        var koffi   = store.FindUserByPhone("+2250701000001");

        if (amadou is null || fatou is null || koffi is null) return;

        var amadouWallet = store.FindWalletById(amadou.WalletId)!;
        var fatouWallet  = store.FindWalletById(fatou.WalletId)!;
        var koffiWallet  = store.FindWalletById(koffi.WalletId)!;

        // Domestic: Amadou → Fatou 15 000 XOF (free)
        var tx1 = new Transaction
        {
            Type            = TransactionType.Transfer,
            Status          = TransactionStatus.Completed,
            SenderWalletId  = amadouWallet.Id,
            SenderPhone     = amadou.PhoneNumber,
            ReceiverWalletId= fatouWallet.Id,
            ReceiverPhone   = fatou.PhoneNumber,
            Amount          = 15_000m,
            Fee             = 0m,
            TotalDeducted   = 15_000m,
            Currency        = "XOF",
            Note            = "Remboursement",
            IsInternational = false,
            CreatedAt       = DateTime.UtcNow.AddDays(-3),
            CompletedAt     = DateTime.UtcNow.AddDays(-3)
        };
        store.AddTransaction(tx1);

        // International: Amadou (SN) → Koffi (CI) 50 000 XOF (1.5% fee = 750)
        var tx2 = new Transaction
        {
            Type            = TransactionType.Transfer,
            Status          = TransactionStatus.Completed,
            SenderWalletId  = amadouWallet.Id,
            SenderPhone     = amadou.PhoneNumber,
            ReceiverWalletId= koffiWallet.Id,
            ReceiverPhone   = koffi.PhoneNumber,
            Amount          = 50_000m,
            Fee             = 750m,
            TotalDeducted   = 50_750m,
            Currency        = "XOF",
            IsInternational = true,
            CreatedAt       = DateTime.UtcNow.AddDays(-1),
            CompletedAt     = DateTime.UtcNow.AddDays(-1)
        };
        store.AddTransaction(tx2);

        // Airtime top-up by Fatou
        var tx3 = new Transaction
        {
            Type            = TransactionType.AirtimeTopUp,
            Status          = TransactionStatus.Completed,
            SenderWalletId  = fatouWallet.Id,
            SenderPhone     = fatou.PhoneNumber,
            ReceiverPhone   = fatou.PhoneNumber,
            Amount          = 2_000m,
            Fee             = 40m,
            TotalDeducted   = 2_040m,
            Currency        = "XOF",
            Note            = "Airtime top-up via Orange",
            CreatedAt       = DateTime.UtcNow.AddHours(-5),
            CompletedAt     = DateTime.UtcNow.AddHours(-5)
        };
        store.AddTransaction(tx3);
    }

    private static decimal GetDailyLimit(string currency) => currency switch
    {
        "UGX" => 10_000_000m,
        "GHS" => 20_000m,
        "GMD" => 75_000m,
        _     => 3_000_000m
    };

    private record SeedUser(
        string Phone, string Name, string Country, string Pin,
        decimal Balance, UserRole Role,
        string? AgentBusiness = null, string? AgentCity = null);
}
