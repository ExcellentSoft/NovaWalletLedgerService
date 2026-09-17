namespace NovaWallet.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using NovaWallet.Domain.Entities;
using NovaWallet.Domain.Enums;
using NovaWallet.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private const string PostgresUniqueViolationSqlState = "23505";

    private readonly NovaWalletDbContext _dbContext;

    //private IWalletRepository? _wallets;
    //private IWalletTransactionRepository? _walletTransactions;
    //private IAuditLogRepository? _auditLogs;
    //private IIdempotencyRecordRepository? _idempotencyRecords;
    //private IDailyTransferUsageRepository? _dailyTransferUsages;

    public UnitOfWork(NovaWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IWalletRepository Wallets =>  new WalletRepository(_dbContext);

    public IWalletTransactionRepository WalletTransactions =>  new WalletTransactionRepository(_dbContext);

    public IAuditLogRepository AuditLogs =>  new AuditLogRepository(_dbContext);

    public IIdempotencyRecordRepository IdempotencyRecords =>   new IdempotencyRecordRepository(_dbContext);

    public IDailyTransferUsageRepository DailyTransferUsages =>  new DailyTransferUsageRepository(_dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
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

    public async Task<IReadOnlyList<Wallet>> LockWalletsAsync(IReadOnlyCollection<Guid> walletIds, CancellationToken cancellationToken = default)
    {
        var ids = walletIds.Distinct().OrderBy(id => id).ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<Wallet>();
        }

        // Locking rows in a deterministic (ascending Id) order prevents deadlocks when two
        // concurrent transfers involve the same pair of wallets in opposite directions.
        return await _dbContext.Wallets
            .FromSqlInterpolated($@"SELECT * FROM ""Wallets"" WHERE ""Id"" = ANY({ids}) ORDER BY ""Id"" FOR UPDATE")
            .ToListAsync(cancellationToken);
    }

    public async Task<DailyTransferUsage> GetOrCreateDailyUsageForUpdateAsync(Guid walletId, DateOnly usageDate, CancellationToken cancellationToken = default)
    {
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
            // Flush immediately: the unique index on (WalletId, UsageDate) guarantees only one row per
            // wallet/day, and this insert also acquires the row lock for the remainder of the transaction.
            await _dbContext.SaveChangesAsync(cancellationToken);
            return usage;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another concurrent transfer for the same wallet/day won the race to insert the row first.
            _dbContext.Entry(usage).State = EntityState.Detached;

            var created = await _dbContext.DailyTransferUsages
                .FromSqlInterpolated($@"SELECT * FROM ""DailyTransferUsages"" WHERE ""WalletId"" = {walletId} AND ""UsageDate"" = {usageDate} FOR UPDATE")
                .ToListAsync(cancellationToken);

            return created[0];
        }
    }

    public async Task<IdempotencyClaim> ClaimIdempotencyKeyAsync(string key, string requestHash, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
            {
                return new IdempotencyClaim(IdempotencyClaimStatus.PayloadMismatch);
            }

            return existing.Status switch
            {
                IdempotencyStatus.Completed => new IdempotencyClaim(IdempotencyClaimStatus.CompletedReplay, existing.ResponsePayload),
                IdempotencyStatus.InProgress => new IdempotencyClaim(IdempotencyClaimStatus.InProgress),
                // A previously failed attempt with the same key/payload is allowed to be retried.
                _ => await TryInsertClaimAsync(key, requestHash, cancellationToken)
            };
        }

        return await TryInsertClaimAsync(key, requestHash, cancellationToken);
    }

    public async Task CompleteIdempotencyKeyAsync(string key, string responsePayload, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
        record?.Complete(responsePayload);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FailIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);
        record?.Fail();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IdempotencyClaim> TryInsertClaimAsync(string key, string requestHash, CancellationToken cancellationToken)
    {
        var record = new IdempotencyRecord(Guid.NewGuid(), key, requestHash);

        try
        {
            await _dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new IdempotencyClaim(IdempotencyClaimStatus.Claimed);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Lost the race to another concurrent request using the same key.
            _dbContext.Entry(record).State = EntityState.Detached;
            return new IdempotencyClaim(IdempotencyClaimStatus.InProgress);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresUniqueViolationSqlState;
    }

    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}

