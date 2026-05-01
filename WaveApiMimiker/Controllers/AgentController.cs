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

    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>Register the authenticated user as a Wave agent</summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterAgentDto dto)
    {
        var (success, error, agent) = _agentService.RegisterAgent(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(agent);
    }

    /// <summary>Look up an agent by their agent code</summary>
    [HttpGet("{agentCode}")]
    public IActionResult GetAgent(string agentCode)
    {
        var (success, error, agent) = _agentService.GetAgent(agentCode);
        if (!success) return NotFound(new { error });
        return Ok(agent);
    }

    /// <summary>List active agents in a country (SN, CI, ML, BF, UG, GM, GH)</summary>
    [HttpGet("country/{countryCode}")]
    public IActionResult GetByCountry(string countryCode)
    {
        var (success, error, agents) = _agentService.GetAgentsByCountry(countryCode);
        if (!success) return BadRequest(new { error });
        return Ok(agents);
    }

    private string UserId()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
