using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaveApiMimiker.Data;
using WaveApiMimiker.DTOs;
using WaveApiMimiker.Services;

namespace WaveApiMimiker.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public class TransferController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly AppDbContext _db;

    public TransferController(ITransferService transferService, AppDbContext db)
    {
        _transferService = transferService;
        _db = db;
    }

    /// <summary>Send money to another Wave user by phone number (free for domestic)</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMoneyDto dto)
    {
        var (success, error, tx) = await _transferService.SendMoneyAsync(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Agent: deposit cash into a customer's wallet (cash-in, free)</summary>
    [HttpPost("cash-in")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> CashIn([FromBody] CashInDto dto)
    {
        var (success, error, tx) = await _transferService.CashInAsync(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Agent: withdraw cash from a customer's wallet (cash-out, 1% fee)</summary>
    [HttpPost("cash-out")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> CashOut([FromBody] CashOutDto dto)
    {
        var (success, error, tx) = await _transferService.CashOutAsync(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Buy mobile airtime for any phone number</summary>
    [HttpPost("airtime")]
    public async Task<IActionResult> Airtime([FromBody] AirtimeDto dto)
    {
        var (success, error, tx) = await _transferService.AirtimeTopUpAsync(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Get transaction by ID or reference number</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(string id)
    {
        var (success, error, tx) = await _transferService.GetTransactionAsync(id, UserId());
        if (!success) return NotFound(new { error });
        return Ok(tx);
    }

    /// <summary>Get paginated transaction history for the authenticated user</summary>
    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (success, error, txs) = await _transferService.GetHistoryAsync(UserId(), page, pageSize);
        if (!success) return BadRequest(new { error });
        return Ok(new { page, pageSize, transactions = txs });
    }

    /// <summary>Calculate fee before executing a transaction</summary>
    [HttpPost("calculate-fee")]
    public async Task<IActionResult> CalculateFee([FromBody] FeeRequestDto dto)
    {
        var user = await _db.Users.FindAsync(UserId());
        if (user is null) return Unauthorized();
        var currency = WaveConstants.CountryCurrencies[user.CountryCode];
        var result = await _transferService.CalculateFeeAsync(dto, user.CountryCode, currency);
        return Ok(result);
    }

    private string UserId()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
