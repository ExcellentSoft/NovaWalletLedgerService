namespace NovaWallet.Application.Dtos;

/// <summary>
/// Request to move funds atomically between two wallets. <see cref="IdempotencyKey"/> must come from the
/// client's Idempotency-Key request header; replaying the same key with the same payload must return the
/// original result, and reusing it with a different payload must be rejected.
/// </summary>
public record TransferRequest(
    Guid FromWalletId,
    Guid ToWalletId,
    long AmountKobo,
    string IdempotencyKey,
    string? Reference = null);
