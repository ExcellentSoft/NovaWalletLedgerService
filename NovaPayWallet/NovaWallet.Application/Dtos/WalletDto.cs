namespace NovaWallet.Application.Dtos;

public record WalletDto(
    Guid Id,
    Guid OwnerId,
    long BalanceKobo,
    string Currency,
    bool IsActive,
    DateTime CreatedAtUtc);
