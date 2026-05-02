using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Services;

namespace WaveApiMimiker.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Register a new Wave account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var (success, error, response) = await _authService.RegisterAsync(dto);
        if (!success) return BadRequest(new { error });
        return Ok(response);
    }

    /// <summary>Login with phone number and PIN</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (success, error, response) = await _authService.LoginAsync(dto);
        if (!success) return Unauthorized(new { error });
        return Ok(response);
    }

    /// <summary>Get current authenticated user profile</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        userId  = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        phone   = User.FindFirst(System.Security.Claims.ClaimTypes.MobilePhone)?.Value,
        role    = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
        country = User.FindFirst("country")?.Value
    });
}
