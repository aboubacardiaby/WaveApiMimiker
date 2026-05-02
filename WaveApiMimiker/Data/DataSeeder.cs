using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Idempotent — skip if any user already exists
        if (await db.Users.AnyAsync()) return;

        var users = new[]
        {
            // ── Senegal (XOF) ──────────────────────────────────────────────
            new SeedUser("+221771000001", "Amadou Diallo",      "SN", 250_000m,  UserRole.Customer),
            new SeedUser("+221772000002", "Fatou Ndiaye",       "SN", 80_000m,   UserRole.Customer),
            new SeedUser("+221773000003", "Moussa Sow",         "SN", 500_000m,  UserRole.Agent,   "Chez Moussa Mobile Money", "Dakar"),
            new SeedUser("+221774000004", "Aïssatou Ba",        "SN", 45_000m,   UserRole.Customer),
            new SeedUser("+221775000005", "Ibrahima Fall",      "SN", 1_200_000m,UserRole.Customer),
            new SeedUser("+221778689865", "Ousmane Diaby",      "SN", 100_000m,  UserRole.Customer),
            new SeedUser("+221773005100", "Aliou Mbaye",        "SN", 100_000m,  UserRole.Customer),
            new SeedUser("+2207878788",   "Cheikh Sarr",        "SN", 100_000m,  UserRole.Customer),

            // ── Côte d'Ivoire (XOF) ───────────────────────────────────────
            new SeedUser("+2250701000001","Koffi Assi",         "CI", 300_000m,  UserRole.Customer),
            new SeedUser("+2250702000002","Aya Touré",          "CI", 150_000m,  UserRole.Customer),
            new SeedUser("+2250703000003","Mamadou Koné",       "CI", 700_000m,  UserRole.Agent,   "Wave Point Cocody", "Abidjan"),

            // ── Mali (XOF) ────────────────────────────────────────────────
            new SeedUser("+22370000001",  "Seydou Keïta",      "ML", 175_000m,  UserRole.Customer),
            new SeedUser("+22371000002",  "Mariam Coulibaly",  "ML", 90_000m,   UserRole.Customer),
            new SeedUser("+22372000003",  "Boubacar Traoré",   "ML", 400_000m,  UserRole.Agent,   "Transfert Rapide Bamako", "Bamako"),

            // ── Burkina Faso (XOF) ────────────────────────────────────────
            new SeedUser("+22670000001",  "Rasmané Ouédraogo", "BF", 60_000m,   UserRole.Customer),
            new SeedUser("+22671000002",  "Adja Sawadogo",     "BF", 220_000m,  UserRole.Customer),

            // ── Uganda (UGX) ──────────────────────────────────────────────
            new SeedUser("+256701000001", "Moses Okonkwo",     "UG", 800_000m,  UserRole.Customer),
            new SeedUser("+256702000002", "Grace Nakato",      "UG", 250_000m,  UserRole.Customer),
            new SeedUser("+256703000003", "John Ssekibuule",   "UG", 2_000_000m,UserRole.Agent,   "Kampala Wave Agency", "Kampala"),

            // ── Gambia (GMD) ──────────────────────────────────────────────
            new SeedUser("+2203000001",   "Lamin Jallow",      "GM", 12_000m,   UserRole.Customer),
            new SeedUser("+2203000002",   "Sainabou Ceesay",   "GM", 5_500m,    UserRole.Customer),
            new SeedUser("+220000009",    "Binta Jammeh",      "GM", 8_000m,    UserRole.Customer),

            // ── Ghana (GHS) ───────────────────────────────────────────────
            new SeedUser("+233501000001", "Kwame Mensah",      "GH", 3_500m,    UserRole.Customer),
            new SeedUser("+233502000002", "Akua Boateng",      "GH", 1_200m,    UserRole.Customer),
            new SeedUser("+233503000003", "Kofi Agyemang",     "GH", 8_000m,    UserRole.Agent,   "Accra Wave Hub", "Accra"),
        };

        foreach (var seed in users)
        {
            var currency = WaveConstants.CountryCurrencies[seed.Country];
            var user = new User
            {
                PhoneNumber  = seed.Phone,
                FullName     = seed.Name,
                CountryCode  = seed.Country,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                Role         = seed.Role,
                Status       = AccountStatus.Active
            };
            var wallet = new Wallet
            {
                OwnerId            = user.Id,
                Currency           = currency,
                Balance            = seed.Balance,
                DailyTransferLimit = GetDailyLimit(currency)
            };
            user.WalletId = wallet.Id;

            db.Users.Add(user);
            db.Wallets.Add(wallet);

            if (seed.Role == UserRole.Agent && seed.AgentBusiness is not null)
            {
                db.Agents.Add(new Agent
                {
                    UserId       = user.Id,
                    AgentCode    = $"{seed.Country}-{seed.Phone[^5..]}",
                    BusinessName = seed.AgentBusiness,
                    CountryCode  = seed.Country,
                    PhoneNumber  = seed.Phone,
                    City         = seed.AgentCity ?? seed.Country,
                    IsActive     = true
                });
            }
        }

        await db.SaveChangesAsync();
        await SeedTransactionsAsync(db);
    }

    private static async Task SeedTransactionsAsync(AppDbContext db)
    {
        var amadou = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "+221771000001");
        var fatou  = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "+221772000002");
        var koffi  = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "+2250701000001");
        if (amadou is null || fatou is null || koffi is null) return;

        var amadouWallet = await db.Wallets.FirstAsync(w => w.Id == amadou.WalletId);
        var fatouWallet  = await db.Wallets.FirstAsync(w => w.Id == fatou.WalletId);
        var koffiWallet  = await db.Wallets.FirstAsync(w => w.Id == koffi.WalletId);

        db.Transactions.AddRange(
            new Transaction
            {
                Type = TransactionType.Transfer, Status = TransactionStatus.Completed,
                SenderWalletId = amadouWallet.Id, SenderPhone = amadou.PhoneNumber,
                ReceiverWalletId = fatouWallet.Id, ReceiverPhone = fatou.PhoneNumber,
                Amount = 15_000m, Fee = 0, TotalDeducted = 15_000m, Currency = "XOF",
                Note = "Remboursement", CreatedAt = DateTime.UtcNow.AddDays(-3),
                CompletedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Transaction
            {
                Type = TransactionType.Transfer, Status = TransactionStatus.Completed,
                SenderWalletId = amadouWallet.Id, SenderPhone = amadou.PhoneNumber,
                ReceiverWalletId = koffiWallet.Id, ReceiverPhone = koffi.PhoneNumber,
                Amount = 50_000m, Fee = 750m, TotalDeducted = 50_750m, Currency = "XOF",
                IsInternational = true, CreatedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Transaction
            {
                Type = TransactionType.AirtimeTopUp, Status = TransactionStatus.Completed,
                SenderWalletId = fatouWallet.Id, SenderPhone = fatou.PhoneNumber,
                ReceiverPhone = fatou.PhoneNumber,
                Amount = 2_000m, Fee = 40m, TotalDeducted = 2_040m, Currency = "XOF",
                Note = "Airtime top-up via Orange", CreatedAt = DateTime.UtcNow.AddHours(-5),
                CompletedAt = DateTime.UtcNow.AddHours(-5)
            }
        );

        await db.SaveChangesAsync();
    }

    private static decimal GetDailyLimit(string currency) => currency switch
    {
        "UGX" => 10_000_000m,
        "GHS" => 20_000m,
        "GMD" => 75_000m,
        _     => 3_000_000m
    };

    private record SeedUser(string Phone, string Name, string Country, decimal Balance,
        UserRole Role, string? AgentBusiness = null, string? AgentCity = null);
}
