using System.ComponentModel.DataAnnotations;

namespace WaveApiMimiker.DTOs;

public class RegisterAgentDto
{
    [Required]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;
}

public class AgentDto
{
    public string Id { get; set; } = string.Empty;
    public string AgentCode { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal TotalCashInToday { get; set; }
    public decimal TotalCashOutToday { get; set; }
}
