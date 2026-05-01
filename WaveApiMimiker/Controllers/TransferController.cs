using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly InMemoryDataStore _store;

    public TransferController(ITransferService transferService, InMemoryDataStore store)
    {
        _transferService = transferService;
        _store = store;
    }

    /// <summary>Send money to another Wave user by phone number (free for domestic)</summary>
    [HttpPost("send")]
    public IActionResult Send([FromBody] SendMoneyDto dto)
    {
        var (success, error, tx) = _transferService.SendMoney(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Agent: deposit cash into a customer's wallet (cash-in, free)</summary>
    [HttpPost("cash-in")]
    [Authorize(Roles = "Agent,Admin")]
    public IActionResult CashIn([FromBody] CashInDto dto)
    {
        var (success, error, tx) = _transferService.CashIn(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Agent: withdraw cash from a customer's wallet (cash-out, 1% fee)</summary>
    [HttpPost("cash-out")]
    [Authorize(Roles = "Agent,Admin")]
    public IActionResult CashOut([FromBody] CashOutDto dto)
    {
        var (success, error, tx) = _transferService.CashOut(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Buy mobile airtime for any phone number</summary>
    [HttpPost("airtime")]
    public IActionResult Airtime([FromBody] AirtimeDto dto)
    {
        var (success, error, tx) = _transferService.AirtimeTopUp(UserId(), dto);
        if (!success) return BadRequest(new { error });
        return Ok(tx);
    }

    /// <summary>Get transaction by ID or reference number</summary>
    [HttpGet("{id}")]
    public IActionResult GetTransaction(string id)
    {
        var (success, error, tx) = _transferService.GetTransaction(id, UserId());
        if (!success) return NotFound(new { error });
        return Ok(tx);
    }

    /// <summary>Get paginated transaction history for the authenticated user</summary>
    [HttpGet("history")]
    public IActionResult History([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (success, error, txs) = _transferService.GetHistory(UserId(), page, pageSize);
        if (!success) return BadRequest(new { error });
        return Ok(new { page, pageSize, transactions = txs });
    }

    /// <summary>Calculate fee before executing a transaction</summary>
    [HttpPost("calculate-fee")]
    public IActionResult CalculateFee([FromBody] FeeRequestDto dto)
    {
        var user = _store.FindUserById(UserId());
        if (user is null) return Unauthorized();
        var currency = InMemoryDataStore.CountryCurrencies[user.CountryCode];
        var result = _transferService.CalculateFee(dto, user.CountryCode, currency);
        return Ok(result);
    }

    private string UserId()
        => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
}
