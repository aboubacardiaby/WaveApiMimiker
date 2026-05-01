using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Services;

namespace WaveApiMimiker.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>Get the authenticated user's wallet balance and limits</summary>
    [HttpGet("balance")]
    public IActionResult GetBalance()
    {
        var userId = UserId();
        var (success, error, wallet) = _walletService.GetWallet(userId);
        if (!success) return NotFound(new { error });
        return Ok(wallet);
    }

    private string UserId()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
