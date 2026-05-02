using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Agent> Agents => Set<Agent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.PhoneNumber).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
            e.Property(u => u.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.OwnerId).IsUnique();
            e.Property(w => w.Balance).HasPrecision(18, 4);
            e.Property(w => w.DailyTransferLimit).HasPrecision(18, 4);
            e.Property(w => w.DailyTransferSent).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Reference).IsUnique();
            e.Property(t => t.Type).HasConversion<string>();
            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Amount).HasPrecision(18, 4);
            e.Property(t => t.Fee).HasPrecision(18, 4);
            e.Property(t => t.TotalDeducted).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Agent>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.AgentCode).IsUnique();
            e.HasIndex(a => a.UserId).IsUnique();
            e.Property(a => a.TotalCashInToday).HasPrecision(18, 4);
            e.Property(a => a.TotalCashOutToday).HasPrecision(18, 4);
        });
    }
}
