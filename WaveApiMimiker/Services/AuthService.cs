using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IAuthService
{
    Task<(bool Success, string Error, AuthResponseDto? Response)> RegisterAsync(RegisterDto dto);
    Task<(bool Success, string Error, AuthResponseDto? Response)> LoginAsync(LoginDto dto);
    string GenerateToken(User user);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWalletService _walletService;

    public AuthService(AppDbContext db, IConfiguration config, IWalletService walletService)
    {
        _db = db;
        _config = config;
        _walletService = walletService;
    }

    public async Task<(bool Success, string Error, AuthResponseDto? Response)> RegisterAsync(RegisterDto dto)
    {
        // Resolve country: use supplied value or auto-detect from phone prefix
        var country = !string.IsNullOrWhiteSpace(dto.CountryCode)
            ? dto.CountryCode.ToUpper()
            : WaveConstants.DetectCountry(dto.PhoneNumber);

        if (country is null)
            return (false, "Could not detect country from phone number. Please supply countryCode (SN, CI, ML, BF, UG, GM, GH).", null);

        if (!WaveConstants.SupportedCountries.Contains(country))
            return (false, $"Country '{country}' is not supported. Supported: SN, CI, ML, BF, UG, GM, GH", null);

        dto.CountryCode = country;

        if (await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            return (false, "Phone number already registered", null);

        var user = new User
        {
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            CountryCode = country,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin),
            Role = UserRole.Customer,
            Status = AccountStatus.Active
        };

        var wallet = await _walletService.CreateWalletAsync(user.Id, user.CountryCode);
        wallet.Balance = GetSeedBalance(user.CountryCode);
        user.WalletId = wallet.Id;

        _db.Users.Add(user);
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();

        return (true, string.Empty, BuildResponse(user, wallet));
    }

    public async Task<(bool Success, string Error, AuthResponseDto? Response)> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Pin, user.PasswordHash))
            return (false, "Invalid phone number or PIN", null);

        if (user.Status == AccountStatus.Suspended)
            return (false, "Account is suspended. Contact support.", null);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var wallet = await _db.Wallets.FirstAsync(w => w.Id == user.WalletId);
        return (true, string.Empty, BuildResponse(user, wallet));
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key missing")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("country", user.CountryCode),
            new Claim("walletId", user.WalletId)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private AuthResponseDto BuildResponse(User user, Wallet wallet) => new()
    {
        Token = GenerateToken(user),
        UserId = user.Id,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        CountryCode = user.CountryCode,
        Role = user.Role.ToString(),
        Wallet = _walletService.MapToDto(wallet)
    };

    private static decimal GetSeedBalance(string countryCode) => countryCode switch
    {
        "UG" => 500_000m,
        "GH" => 2_000m,
        "GM" => 9_000m,
        _ => 100_000m
    };
}
