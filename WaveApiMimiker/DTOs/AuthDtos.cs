using System.ComponentModel.DataAnnotations;

namespace WaveApiMimiker.DTOs;

public class RegisterDto
{
    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(4)]
    public string Pin { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    // Optional — auto-detected from phone prefix if omitted (e.g. +220 → GM)
    public string? CountryCode { get; set; }
}

public class LoginDto
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Pin { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public WalletDto Wallet { get; set; } = new();
}
