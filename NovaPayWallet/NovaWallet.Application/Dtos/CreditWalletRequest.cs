namespace NovaWallet.Application.Dtos;

/// <summary>Request to credit a wallet, e.g. simulating an inbound NIBSS NIP transfer.</summary>
public record CreditWalletRequest(Guid WalletId, long AmountKobo, string? Reference = null);
