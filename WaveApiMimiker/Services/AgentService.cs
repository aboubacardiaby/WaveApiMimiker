using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Models;

namespace WaveApiMimiker.Services;

public interface IAgentService
{
    (bool Success, string Error, AgentDto? Agent) RegisterAgent(string userId, RegisterAgentDto dto);
    (bool Success, string Error, AgentDto? Agent) GetAgent(string agentCode);
    (bool Success, string Error, List<AgentDto> Agents) GetAgentsByCountry(string countryCode);
}

public class AgentService : IAgentService
{
    private readonly InMemoryDataStore _store;

    public AgentService(InMemoryDataStore store)
    {
        _store = store;
    }

    public (bool Success, string Error, AgentDto? Agent) RegisterAgent(string userId, RegisterAgentDto dto)
    {
        var user = _store.FindUserById(userId);
        if (user is null) return (false, "User not found", null);

        if (_store.FindAgentByUserId(userId) is not null)
            return (false, "This account is already registered as an agent", null);

        // Promote user role
        user.Role = UserRole.Agent;
        _store.UpdateUser(user);

        var agent = new Agent
        {
            UserId = userId,
            AgentCode = GenerateAgentCode(user.CountryCode),
            BusinessName = dto.BusinessName,
            CountryCode = user.CountryCode,
            PhoneNumber = user.PhoneNumber,
            City = dto.City,
            IsActive = true
        };
        _store.AddAgent(agent);

        return (true, string.Empty, MapToDto(agent));
    }

    public (bool Success, string Error, AgentDto? Agent) GetAgent(string agentCode)
    {
        var agent = _store.FindAgentByCode(agentCode);
        if (agent is null) return (false, "Agent not found", null);
        return (true, string.Empty, MapToDto(agent));
    }

    public (bool Success, string Error, List<AgentDto> Agents) GetAgentsByCountry(string countryCode)
    {
        if (!InMemoryDataStore.SupportedCountries.Contains(countryCode.ToUpper()))
            return (false, $"Unsupported country: {countryCode}", new());

        var agents = _store.GetAgentsByCountry(countryCode.ToUpper()).Select(MapToDto).ToList();
        return (true, string.Empty, agents);
    }

    private static string GenerateAgentCode(string country)
        => $"{country}-{Random.Shared.Next(10000, 99999)}";

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
