# AI Usage

This is how I used AI for coding assistants. 

## Tools used

| Tool | Role |
|---|---|
| **GitHub Copilot (agent mode, Visual Studio)** | Primary assistant for day-to-day development/code implementation: Docker/build troubleshooting, and reviewing business logic in the Application and Infrastructure layers. |

AI Copilo was interactively throughout the project and used for debugging,
for scaffolding and reviewing the wallet transfer logic, and for writing developer-facing
documentation. I make sure i reviewed every suggested code, tested, and corrected some where necessary before it is accepted.
 

## Prompt log

### 1. Implementing the Unit of Work with row-locking and transactional helpers

**Prompt:**
"Implement UnitOfWork for IUnitOfWork on top of NovaWalletDbContext. It needs to expose
 the repositories, a SaveChangesAsync, and an ExecuteInTransactionAsync that wraps an
 operation in a DB transaction using EF Core's execution strategy. Also add a LockWalletsAsync(IEnumerable<Guid> walletIds) that pessimistically
 locks the given wallet rows for update, in a way that avoids deadlocks when two transfers touch
 the same two wallets in opposite order."

**What came back: AI response code**
 
 "
namespace NovaWallet.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using NovaWallet.Domain.Entities;
using NovaWallet.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly NovaWalletDbContext _dbContext;

    public UnitOfWork(NovaWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IWalletRepository Wallets => new WalletRepository(_dbContext);

    public IWalletTransactionRepository WalletTransactions => new WalletTransactionRepository(_dbContext);

    public IAuditLogRepository AuditLogs => new AuditLogRepository(_dbContext);

    public IIdempotencyRecordRepository IdempotencyRecords => new IdempotencyRecordRepository(_dbContext);

    public IDailyTransferUsageRepository DailyTransferUsages => new DailyTransferUsageRepository(_dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        // Npgsql's execution strategy retries the whole delegate (including BeginTransaction)
        // on transient failures, so the transaction must be created *inside* the strategy call.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<IReadOnlyList<Wallet>> LockWalletsAsync(
        IReadOnlyCollection<Guid> walletIds,
        CancellationToken cancellationToken = default)
    {
        // Sorting the requested IDs ascending guarantees that no matter which wallet a caller
        // labels "from" or "to", two concurrent transfers touching the same pair of wallets
        // always acquire their row locks in the same order — eliminating deadlocks.
        var ids = walletIds.Distinct().OrderBy(id => id).ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<Wallet>();
        }

        return await _dbContext.Wallets
            .FromSqlInterpolated($@"SELECT * FROM ""Wallets"" WHERE ""Id"" = ANY({ids}) ORDER BY ""Id"" FOR UPDATE")
            .ToListAsync(cancellationToken);
    }
}
"

**Outcome:** The code was Accepted with light reviews.


### 2. Idempotent daily-usage row creation under concurrent inserts

**Prompt:**
"Add GetOrCreateDailyUsageForUpdateAsync(Guid walletId, DateOnly usageDate) to UnitOfWork.
There's a unique index on (WalletId, UsageDate) in DailyTransferUsage. If the row doesn't
exist yet, create and lock it; if two concurrent transfers for the same wallet on the same day
both try to create it, only one should succeed and the other should just read the row"


**What came back: AI response code**
 
 "public async Task<DailyTransferUsage> GetOrCreateDailyUsageForUpdateAsync(
    Guid walletId,
    DateOnly usageDate,
    CancellationToken cancellationToken = default)
{
    // Try to read (and lock) an existing row first — the common case after the first
    // transfer of the day for this wallet.
    var existing = await _dbContext.DailyTransferUsages
        .FromSqlInterpolated($@"SELECT * FROM ""DailyTransferUsages"" WHERE ""WalletId"" = {walletId} AND ""UsageDate"" = {usageDate} FOR UPDATE")
        .ToListAsync(cancellationToken);

    if (existing.Count > 0)
    {
        return existing[0];
    }

    var usage = new DailyTransferUsage(Guid.NewGuid(), walletId, usageDate);
    await _dbContext.DailyTransferUsages.AddAsync(usage, cancellationToken);

    try
    {
        // Flush immediately: this both creates the row and acquires its lock for the
        // remainder of the surrounding transaction, so subsequent updates within the same
        // transfer are consistent.
        await _dbContext.SaveChangesAsync(cancellationToken);
        return usage;
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        // Another concurrent transfer for the same wallet/day won the race to insert first.
        // Detach our losing in-memory entity and re-read the row the winner committed.
        _dbContext.Entry(usage).State = EntityState.Detached;

        var created = await _dbContext.DailyTransferUsages
            .FromSqlInterpolated($@"SELECT * FROM ""DailyTransferUsages"" WHERE ""WalletId"" = {walletId} AND ""UsageDate"" = {usageDate} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return created[0];
    }
}

private static bool IsUniqueViolation(DbUpdateException ex)
{
    // Postgres SQLSTATE 23505 = unique_violation, raised when the (WalletId, UsageDate)
    // unique index rejects the second concurrent insert.
    return ex.InnerException is PostgresException pgEx
        && pgEx.SqlState == PostgresUniqueViolationSqlState;
}"

Usage inside the transfer flow...
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    var wallets = await _unitOfWork.LockWalletsAsync(new[] { fromWalletId, toWalletId }, ct);

    // Row is created-and-locked, or an existing row is locked — either way, the daily total
    // read here can't be changed by another concurrent transfer until this transaction commits.
    var usage = await _unitOfWork.GetOrCreateDailyUsageForUpdateAsync(fromWalletId, DateOnly.FromDateTime(DateTime.UtcNow), ct);

    if (usage.TotalOutboundKobo + amountKobo > _options.DailyOutboundTransferLimitKobo)
    {
        throw new DailyTransferLimitExceededException(fromWalletId, usage.TotalOutboundKobo, amountKobo);
    }

    usage.AddOutbound(amountKobo);
    // ... debit/credit wallets, write transaction + audit log ...
}, cancellationToken);

**Outcome:**  The code was Accepted with light reviews



### 3. AI output was wrong/unsafe : Code Reviewed.   

This case most relevant to a financial system, AI suggestion looked ok but it has potential to
introduced a way to loose money.

**Prompt:**
"Add a TransferAsync method to WalletService that debits FromWalletId and credits
ToWalletId for the same amount,save transaction record and returns a TransferResponse."

**What came back (first draft, problematic):**
 AI response,  the two wallets independently via
`_unitOfWork.Wallets.GetByIdAsync(fromId)` and `GetByIdAsync(toId)`, checked
`from.Balance >= amount` in application code, called `from.Debit(amount)` / `to.Credit(amount)`,
and then a single `SaveChangesAsync()` — with **no explicit transaction and no row-level locking**.

## AI Response  Code
...
    public async Task<ResponseResult<TransferResponse>> TransferAsync(
    TransferRequest request,
    CancellationToken cancellationToken = default)
{
    var from = await _unitOfWork.Wallets.GetByIdAsync(request.FromWalletId, cancellationToken);
    var to = await _unitOfWork.Wallets.GetByIdAsync(request.ToWalletId, cancellationToken);

    if (from is null || to is null)
    {       
        return ResponseResult<TransferResponse>.Failure(ErrorType.NotFound, "Wallet not found.");
    }

    // Two concurrent requests can both read the same `from.Balance` here, before either
    // commits the check below is not atomic with the debit that follows.
    if (from.Balance < request.AmountKobo)
    {
        return ResponseResult<TransferResponse>.Failure(ErrorType.Validation, "Insufficient funds.");
    }

    // No IdempotencyKey check: retrying this request (e.g. after a client timeout) moves the
    // funds a second time.
    from.Debit(request.AmountKobo);
    to.Credit(request.AmountKobo);

    _unitOfWork.Wallets.Update(from);
    _unitOfWork.Wallets.Update(to);

    // Single SaveChangesAsync with no surrounding BEGIN/COMMIT transaction and no row locks
    // (e.g. `SELECT ... FOR UPDATE`) — a crash here, or a second concurrent transfer racing
    // against this one, can leave the ledger unbalanced or allow a double-spend.
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return ResponseResult<TransferResponse>.Success(
        new TransferResponse(from.Id, to.Id, request.AmountKobo, from.Balance, to.Balance));
}


**The code is wrong for financial system: because**

**Concurrency / double-spend bug:** When Two concurrent transfer requests debiting the *same* source
  wallet would both read the same starting balance before either write committed (a classic
  read-modify-write race). 
  Both  pass the `Balance >= amount` check and both would debit,
  allowing the wallet to go negative or losing one of the debits depending on EF Core's
  last-write-wins behavior — a real fund-safety issue, not just a cosmetic bug.
 **No idempotency handling:** a client retry (e.g. after a timeout) of the same transfer would
  be processed twice, double-moving funds, since nothing keyed on a client-supplied
  idempotency token.
 **Partial failure risk:** debit and credit were two separate mutations with only an implicit
  reliance on `SaveChangesAsync` batching them, there was no `BEGIN TRANSACTION`, so a crash
  between the debit and credit (or a partial DB failure) could leave the ledger unbalanced.

**How it was caught:**
This was caught during manual review, the question asked is "What happens if two transfers
from the same wallet run at the same time?",   this surfaced the missing locking and the missing
idempotency guard.

**How it was fixed:**
 I add:  `IUnitOfWork.ExecuteInTransactionAsync` so the whole transfer (debit, credit,
  transaction records, audit log, daily-usage update) commits atomically or not at all.
 Added `IUnitOfWork.LockWalletsAsync`, which locks both wallets **in a consistent `Id` order**
  (to avoid deadlocks between two transfers that touch the same pair of wallets in opposite
  order) before any balance is read or mutated, closing the read-modify-write race.
 The for endpoint header an `Idempotency-Key` transfer endpoint is added,
   so a
  retried request with the same key and payload replays the cached result instead of moving funds
  twice, and a retried request with a different payload is rejected as a conflict.

 Also, i make sure the balances long integer to allow kobo (`long`) rather than `decimal`/`double` NGN throughout, to
  avoid a related but distinct class of AI-suggested rounding issues seen in early drafts (e.g.
  `decimal` division when converting between NGN and kobo for display).

**Outcome:** I rewrite transfer flow (`WalletService.TransferAsync` / `ProcessTransferAsync`) with  idempotency-key and locking inside database.

 

## Summary

Sincerely using AI tools is good and make work fast but require professional prompts and reviews and test before finally agree.   I used it for business logic drafts, generate implementation codes. it is useful but i did not trust it. I make sure all codes generated carefully reviewed, and testted, to be sure that the logical outputs is exactly what is expecting from the code.

 
