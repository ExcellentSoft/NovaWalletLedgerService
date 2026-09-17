using NovaWallet.Application.Common;
using NovaWallet.Application.Dtos;

namespace NovaWallet.Application.Interfaces;

public interface IWalletService
{
    /// <summary>Creates a wallet for a customer with a zero starting balance.</summary>
    Task<ResponseResult<WalletDto>> CreateWalletAsync(CreateWalletRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the current balance (in kobo) and currency of a wallet.</summary>
    Task<ResponseResult<WalletBalanceDto>> GetBalanceAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Deposits funds into a wallet, simulating an inbound NIP transfer.</summary>
    Task<ResponseResult<TransactionDto>> CreditWalletAsync(CreditWalletRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically moves funds from one wallet to another. Concurrency-safe (row-level locking prevents
    /// negative balances / double-spend under concurrent load), idempotent (via Idempotency-Key), and
    /// enforces a per-wallet daily outbound transfer limit that resets at midnight WAT.
    /// </summary>
    Task<ResponseResult<TransferResponse>> TransferAsync(TransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a page of a wallet's transaction history, newest first.</summary>
    Task<ResponseResult<PagedResult<TransactionDto>>> GetStatementAsync(GetStatementRequest request, CancellationToken cancellationToken = default);
}
