using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Services;

namespace WaveApiMimiker.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentController(IAgentService agentService) => _agentService = agentService;

    /// <summary>Register the authenticated user as a Wave agent</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAgentDto dto)
    {
        var (success, error, agent) = await _agentService.RegisterAgentAsync(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(agent);
    }

    /// <summary>Look up an agent by their agent code</summary>
    [HttpGet("{agentCode}")]
    public async Task<IActionResult> GetAgent(string agentCode)
    {
        var (success, error, agent) = await _agentService.GetAgentAsync(agentCode);
        if (!success) return NotFound(new { error });
        return Ok(agent);
    }

    /// <summary>List active agents in a country (SN, CI, ML, BF, UG, GM, GH)</summary>
    [HttpGet("country/{countryCode}")]
    public async Task<IActionResult> GetByCountry(string countryCode)
    {
        var (success, error, agents) = await _agentService.GetAgentsByCountryAsync(countryCode);
        if (!success) return BadRequest(new { error });
        return Ok(agents);
    }

    private string UserId()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
