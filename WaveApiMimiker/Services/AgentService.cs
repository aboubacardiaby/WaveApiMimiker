using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IAgentService
{
    Task<(bool Success, string Error, AgentDto? Agent)> RegisterAgentAsync(string userId, RegisterAgentDto dto);
    Task<(bool Success, string Error, AgentDto? Agent)> GetAgentAsync(string agentCode);
    Task<(bool Success, string Error, List<AgentDto> Agents)> GetAgentsByCountryAsync(string countryCode);
}

public class AgentService : IAgentService
{
    private readonly AppDbContext _db;

    public AgentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Error, AgentDto? Agent)> RegisterAgentAsync(string userId, RegisterAgentDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, "User not found", null);

        if (await _db.Agents.AnyAsync(a => a.UserId == userId))
            return (false, "This account is already registered as an agent", null);

        user.Role = UserRole.Agent;

        var agent = new Agent
        {
            UserId = userId,
            AgentCode = $"{user.CountryCode}-{user.PhoneNumber[^5..]}",
            BusinessName = dto.BusinessName,
            CountryCode = user.CountryCode,
            PhoneNumber = user.PhoneNumber,
            City = dto.City,
            IsActive = true
        };
        _db.Agents.Add(agent);
        await _db.SaveChangesAsync();

        return (true, string.Empty, MapToDto(agent));
    }

    public async Task<(bool Success, string Error, AgentDto? Agent)> GetAgentAsync(string agentCode)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.AgentCode == agentCode);
        if (agent is null) return (false, "Agent not found", null);
        return (true, string.Empty, MapToDto(agent));
    }

    public async Task<(bool Success, string Error, List<AgentDto> Agents)> GetAgentsByCountryAsync(string countryCode)
    {
        if (!WaveConstants.SupportedCountries.Contains(countryCode.ToUpper()))
            return (false, $"Unsupported country: {countryCode}", new());

        var agents = await _db.Agents
            .Where(a => a.CountryCode == countryCode.ToUpper() && a.IsActive)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return (true, string.Empty, agents);
    }

    private static AgentDto MapToDto(Agent a) => new()
    {
        Id = a.Id,
        AgentCode = a.AgentCode,
        BusinessName = a.BusinessName,
        CountryCode = a.CountryCode,
        PhoneNumber = a.PhoneNumber,
        City = a.City,
        IsActive = a.IsActive,
        TotalCashInToday = a.TotalCashInToday,
        TotalCashOutToday = a.TotalCashOutToday
    };
}
