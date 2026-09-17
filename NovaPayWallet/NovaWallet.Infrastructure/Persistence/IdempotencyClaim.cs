namespace NovaWallet.Infrastructure.Persistence;

public enum IdempotencyClaimStatus
{
    /// <summary>A brand-new key was claimed by the caller; the caller must process the request and then call CompleteIdempotencyKeyAsync/FailIdempotencyKeyAsync.</summary>
    Claimed,

    /// <summary>The same key was already used with a different request payload. The caller must reject the request (HTTP 409/422).</summary>
    PayloadMismatch,

    /// <summary>The same key is currently being processed by another request. The caller must reject/retry-later (HTTP 409).</summary>
    InProgress,

    /// <summary>The same key already completed successfully with the same payload. The caller must return the cached response (HTTP 200) instead of reprocessing.</summary>
    CompletedReplay
}

/// <summary>
/// Result of attempting to claim an Idempotency-Key for a mutating operation (e.g. a transfer).
/// </summary>
public sealed record IdempotencyClaim(IdempotencyClaimStatus Status, string? CachedResponsePayload = null);
