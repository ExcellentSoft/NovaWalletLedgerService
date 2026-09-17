namespace NovaWallet.Application.Dtos;

public record TransferResponse(
    Guid TransferId,
    Guid FromWalletId,
    Guid ToWalletId,
    long AmountKobo,
    long FromWalletBalanceKobo,
    long ToWalletBalanceKobo,
    DateTime CompletedAtUtc);
