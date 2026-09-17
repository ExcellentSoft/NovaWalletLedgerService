namespace NovaWallet.Application.Dtos;

/// <summary>Request for creating a wallet. Starting balance is always zero.</summary>
public record CreateWalletRequest(Guid CustomerId, string Currency = "NGN");
