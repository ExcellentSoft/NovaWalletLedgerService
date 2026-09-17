namespace NovaWallet.Application.Dtos;

/// <summary>
/// Response for the "Get wallet balance" use case. Amounts are expressed in kobo (1 Naira = 100 kobo)
/// to avoid floating-point drift; <see cref="BalanceNaira"/> is a convenience display value only.
/// </summary>
public record WalletBalanceDto(
    Guid WalletId,
    long BalanceKobo,
    decimal BalanceNaira,
    string Currency);
