using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IAuthService
{
    (bool Success, string Error, AuthResponseDto? Response) Register(RegisterDto dto);
    (bool Success, string Error, AuthResponseDto? Response) Login(LoginDto dto);
    string GenerateToken(User user);
}

public class AuthService : IAuthService
{
    private readonly InMemoryDataStore _store;
    private readonly IConfiguration _config;
    private readonly IWalletService _walletService;

    public AuthService(InMemoryDataStore store, IConfiguration config, IWalletService walletService)
    {
        _store = store;
        _config = config;
        _walletService = walletService;
    }

    public (bool Success, string Error, AuthResponseDto? Response) Register(RegisterDto dto)
    {
        if (!InMemoryDataStore.SupportedCountries.Contains(dto.CountryCode.ToUpper()))
            return (false, $"Country '{dto.CountryCode}' is not supported. Supported: SN, CI, ML, BF, UG, GM, GH", null);

        if (_store.FindUserByPhone(dto.PhoneNumber) is not null)
            return (false, "Phone number already registered", null);

        var user = new User
        {
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            CountryCode = dto.CountryCode.ToUpper(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin),
            Role = UserRole.Customer,
            Status = AccountStatus.Active
        };

        var wallet = _walletService.CreateWallet(user.Id, user.CountryCode);
        user.WalletId = wallet.Id;

        // Seed test balance so the wallet is immediately usable
        wallet.Balance = GetSeedBalance(user.CountryCode);
        _store.AddUser(user);

        return (true, string.Empty, BuildResponse(user, wallet));
    }

    public (bool Success, string Error, AuthResponseDto? Response) Login(LoginDto dto)
    {
        var user = _store.FindUserByPhone(dto.PhoneNumber);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Pin, user.PasswordHash))
            return (false, "Invalid phone number or PIN", null);

        if (user.Status == AccountStatus.Suspended)
            return (false, "Account is suspended. Contact support.", null);

        user.LastLoginAt = DateTime.UtcNow;
        _store.UpdateUser(user);

        var wallet = _store.FindWalletById(user.WalletId)!;
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
        "UG" => 500_000m,  // ~130 USD in UGX
        "GH" => 2_000m,    // ~130 USD in GHS
        "GM" => 9_000m,    // ~130 USD in GMD
        _ => 100_000m      // ~150 USD in XOF
    };
}
