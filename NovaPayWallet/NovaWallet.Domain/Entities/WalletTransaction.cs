using NovaWallet.Domain.Enums;

namespace NovaWallet.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public Guid? RelatedWalletId { get; private set; }
    public string? Reference { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private WalletTransaction()
    {
        Currency = string.Empty;
    }

    public WalletTransaction(
        Guid id,
        Guid walletId,
        TransactionType type,
        decimal amount,
        string currency,
        Guid? relatedWalletId = null,
        string? reference = null)
    {
        Id = id;
        WalletId = walletId;
        Type = type;
        Amount = amount;
        Currency = currency;
        RelatedWalletId = relatedWalletId;
        Reference = reference;
        Status = TransactionStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = TransactionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = TransactionStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkReversed()
    {
        Status = TransactionStatus.Reversed;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
