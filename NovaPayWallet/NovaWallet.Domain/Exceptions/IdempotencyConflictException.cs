namespace NovaWallet.Domain.Exceptions;

public class IdempotencyConflictException : DomainException
{
    public string IdempotencyKey { get; }

    public IdempotencyConflictException(string idempotencyKey)
        : base($"An operation with idempotency key '{idempotencyKey}' is already in progress or has a conflicting result.")
    {
        IdempotencyKey = idempotencyKey;
    }
}
