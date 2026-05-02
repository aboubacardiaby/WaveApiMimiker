using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Data;
using WaveApiMimiker.Services;

namespace WaveApiMimiker.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWalletService _walletService;

    public WalletController(AppDbContext db, IWalletService walletService)
    {
        _db = db;
        _walletService = walletService;
    }

    /// <summary>Get the authenticated user's wallet balance and limits</summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = UserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { error = "User not found" });

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == user.WalletId);
        if (wallet is null) return NotFound(new { error = "Wallet not found" });

        await _walletService.ResetDailyLimitIfNeededAsync(wallet, _db);
        return Ok(_walletService.MapToDto(wallet));
    }

    private string UserId()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
