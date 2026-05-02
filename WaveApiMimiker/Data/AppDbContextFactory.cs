using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WaveApiMimiker.Data;

// Used by EF Core CLI tools (dotnet ef migrations ...) at design time.
// Reads the connection string from an env var so CI never needs a live DB.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=wave_mimiker;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new AppDbContext(options);
    }
}
