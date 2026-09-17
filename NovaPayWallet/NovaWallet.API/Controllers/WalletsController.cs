using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaWallet.Application.Common;
using NovaWallet.Application.Dtos;
using NovaWallet.Application.Interfaces;

namespace NovaWallet.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallets")]
public class WalletsController(IWalletService walletService) : ControllerBase
{
    private readonly IWalletService _walletService = walletService;

    /// <summary>Creates a wallet for a customer id; starting balance is always zero.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request, CancellationToken cancellationToken)
    {
        var result = await _walletService.CreateWalletAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBalance), new { walletId = result.Value!.Id }, result.Value)
            : ToActionResult(result.ErrorType, result.Error);
    }

    /// <summary>Returns the current balance (in kobo) and currency (NGN) of a wallet.</summary>
    [HttpGet("{walletId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid walletId, CancellationToken cancellationToken)
    {
        var result = await _walletService.GetBalanceAsync(walletId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result.ErrorType, result.Error);
    }

    /// <summary>Deposits funds into a wallet, simulating an inbound NIBSS NIP transfer.</summary>
    [HttpPost("{walletId:guid}/credit")]
    public async Task<IActionResult> CreditWallet(Guid walletId, [FromBody] CreditWalletBody body, CancellationToken cancellationToken)
    {
        var request = new CreditWalletRequest(walletId, body.AmountKobo, body.Reference);
        var result = await _walletService.CreditWalletAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result.ErrorType, result.Error);
    }

    /// <summary>
    /// Atomically transfers funds between wallets. Requires an Idempotency-Key header; replaying the same
    /// key with the same payload returns the original result, while reusing it with a different payload is
    /// rejected. Enforces a server-side daily outbound transfer limit that resets at midnight WAT.
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferBody body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new { error = "Idempotency-Key header is required." });
        }

        var request = new TransferRequest(body.FromWalletId, body.ToWalletId, body.AmountKobo, idempotencyKey, body.Reference);
        var result = await _walletService.TransferAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result.ErrorType, result.Error);
    }

    /// <summary>Returns a paginated transaction history for a wallet, newest first.</summary>
    [HttpGet("{walletId:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid walletId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var request = new GetStatementRequest(walletId, page, pageSize);
        var result = await _walletService.GetStatementAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToActionResult(result.ErrorType, result.Error);
    }

    private IActionResult ToActionResult(ErrorType errorType, string? error)
    {
        var payload = new { error };
        return errorType switch
        {
            ErrorType.Validation => BadRequest(payload),
            ErrorType.NotFound => NotFound(payload),
            ErrorType.Conflict => Conflict(payload),
            _ => StatusCode(StatusCodes.Status500InternalServerError, payload)
        };
    }
}


