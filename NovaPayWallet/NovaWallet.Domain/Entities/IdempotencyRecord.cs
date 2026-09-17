using NovaWallet.Domain.Enums;

namespace NovaWallet.Domain.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; }

    /// <summary>
    /// SHA-256 hash (hex) of the canonical request payload that first used this key.
    /// Used to detect a replay of the same key with a different payload, which must be rejected.
    /// </summary>
    public string RequestHash { get; private set; }
    public IdempotencyStatus Status { get; private set; }
    public string? ResponsePayload { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private IdempotencyRecord()
    {
        Key = string.Empty;
        RequestHash = string.Empty;
    }

    public IdempotencyRecord(Guid id, string key, string requestHash)
    {
        Id = id;
        Key = key;
        RequestHash = requestHash;
        Status = IdempotencyStatus.InProgress;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Complete(string? responsePayload)
    {
        Status = IdempotencyStatus.Completed;
        ResponsePayload = responsePayload;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail()
    {
        Status = IdempotencyStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
