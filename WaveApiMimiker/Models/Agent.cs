namespace WaveApiMimiker.Models;

public class Agent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string AgentCode { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal TotalCashInToday { get; set; } = 0;
    public decimal TotalCashOutToday { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
