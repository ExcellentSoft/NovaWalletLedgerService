namespace NovaWallet.Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public Guid WalletId { get; }
    public decimal AvailableBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(Guid walletId, decimal availableBalance, decimal requestedAmount)
        : base($"Wallet {walletId} has insufficient funds. Available: {availableBalance}, Requested: {requestedAmount}")
    {
        WalletId = walletId;
        AvailableBalance = availableBalance;
        RequestedAmount = requestedAmount;
    }
}
