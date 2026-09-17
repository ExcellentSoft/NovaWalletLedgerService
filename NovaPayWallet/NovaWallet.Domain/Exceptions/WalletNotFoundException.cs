namespace NovaWallet.Domain.Exceptions;

public class WalletNotFoundException : DomainException
{
    public Guid WalletId { get; }

    public WalletNotFoundException(Guid walletId)
        : base($"Wallet {walletId} was not found.")
    {
        WalletId = walletId;
    }
}
