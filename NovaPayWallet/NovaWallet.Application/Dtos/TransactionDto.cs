using NovaWallet.Domain.Enums;

namespace NovaWallet.Application.Dtos;

public record TransactionDto(
    Guid Id,
    Guid WalletId,
    TransactionType Type,
    TransactionStatus Status,
    long AmountKobo,
    string Currency,
    Guid? RelatedWalletId,
    string? Reference,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);
