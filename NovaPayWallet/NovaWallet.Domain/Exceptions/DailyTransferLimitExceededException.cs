namespace NovaWallet.Domain.Exceptions;

public class DailyTransferLimitExceededException : DomainException
{
    public Guid WalletId { get; }
    public decimal DailyLimit { get; }
    public decimal AttemptedTotal { get; }

    public DailyTransferLimitExceededException(Guid walletId, decimal dailyLimit, decimal attemptedTotal)
        : base($"Wallet {walletId} exceeded daily transfer limit. Limit: {dailyLimit}, Attempted total: {attemptedTotal}")
    {
        WalletId = walletId;
        DailyLimit = dailyLimit;
        AttemptedTotal = attemptedTotal;
    }
}
