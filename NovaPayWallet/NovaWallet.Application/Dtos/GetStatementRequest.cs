namespace NovaWallet.Application.Dtos;

public record GetStatementRequest(Guid WalletId, int Page = 1, int PageSize = 20);
