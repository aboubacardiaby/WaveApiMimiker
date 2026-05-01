namespace WaveApiMimiker.Models;

public enum UserRole { Customer, Agent, Admin }
public enum AccountStatus { Active, Suspended, PendingVerification }

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty; // SN, CI, ML, BF, UG, GM, GH
    public UserRole Role { get; set; } = UserRole.Customer;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public string WalletId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
