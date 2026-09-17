# Persistence Layer

This folder contains the EF Core persistence implementation for NovaWallet, targeting PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.

- `NovaWalletDbContext` — the EF Core `DbContext` exposing `DbSet<T>` for each domain entity.
- `Configurations/` — `IEntityTypeConfiguration<T>` classes describing table/column mapping for each entity.
- `Repositories/` — one repository (interface + implementation) per aggregate/entity, responsible only for querying and staging changes (`Add`/`Update`) against the `DbContext`.
- `IUnitOfWork` / `UnitOfWork` — coordinates all repositories and commits changes atomically. See below.
- `Migrations/` — EF Core migrations (see `Migrations/README.md`).

## #UseOfUnitOfwork

### The problem before Unit of Work

Previously, every repository (`WalletRepository`, `WalletTransactionRepository`, `AuditLogRepository`, etc.) took its own `NovaWalletDbContext` and exposed its own `SaveChangesAsync`:

```csharp
public class AuditLogRepository : IAuditLogRepository
{
	private readonly NovaWalletDbContext _dbContext;

	public AuditLogRepository(NovaWalletDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> _dbContext.SaveChangesAsync(cancellationToken);
}
```

Because `DbContext` is registered as **scoped**, all repositories used within the same HTTP request/operation actually shared the *same* underlying `DbContext` instance. However, exposing `SaveChangesAsync` on every repository:

- Made it unclear **which repository** was responsible for committing a multi-entity operation (e.g., a money transfer that touches `Wallet`, `WalletTransaction`, `DailyTransferUsage`, and `AuditLog` all at once).
- Encouraged calling `SaveChangesAsync()` multiple times inside one business operation, breaking atomicity — a failure midway could leave the database in a partially-committed state.
- Duplicated the same constructor/field boilerplate (`_dbContext`, `SaveChangesAsync`) across every repository.

### The Unit of Work pattern

A `UnitOfWork` wraps a single `NovaWalletDbContext` and exposes each repository as a lazily-created property, plus a single `SaveChangesAsync`:

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
	IWalletRepository Wallets { get; }
	IWalletTransactionRepository WalletTransactions { get; }
	IAuditLogRepository AuditLogs { get; }
	IIdempotencyRecordRepository IdempotencyRecords { get; }
	IDailyTransferUsageRepository DailyTransferUsages { get; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Consumers (application services / use cases) now depend on `IUnitOfWork` instead of individual repositories:

```csharp
public async Task TransferAsync(Guid fromWalletId, Guid toWalletId, decimal amount, CancellationToken ct)
{
	var from = await _unitOfWork.Wallets.GetByIdAsync(fromWalletId, ct);
	var to = await _unitOfWork.Wallets.GetByIdAsync(toWalletId, ct);

	from!.Debit(amount);
	to!.Credit(amount);

	var transaction = new WalletTransaction(Guid.NewGuid(), fromWalletId, TransactionType.TransferOut, amount, from.Currency, toWalletId);
	await _unitOfWork.WalletTransactions.AddAsync(transaction, ct);

	_unitOfWork.Wallets.Update(from);
	_unitOfWork.Wallets.Update(to);

	// Single commit for the whole business operation
	await _unitOfWork.SaveChangesAsync(ct);
}
```

### Advantages over a `SaveChangesAsync` on every repository

1. **Atomicity / transactional consistency** — All changes made across multiple repositories in one business operation are flushed to the database in a single `SaveChanges` call (and can be wrapped in an explicit DB transaction if needed), so a failure rolls back everything instead of leaving partial writes.
2. **Single Responsibility** — Repositories only know how to query/stage changes for *their* entity; committing/persisting is centralized in one place (`UnitOfWork`), matching its intended role.
3. **Less boilerplate** — No need to inject `NovaWalletDbContext` and repeat a `SaveChangesAsync` wrapper in every repository class.
4. **Clear commit point** — Application/service code has one obvious place (`_unitOfWork.SaveChangesAsync`) to call once all changes for an operation have been staged, making the code easier to reason about and review.
5. **Easier testing** — Application services can be tested against a single `IUnitOfWork` mock/fake instead of mocking `SaveChangesAsync` on every repository involved in a use case.
6. **Consistent `DbContext` lifetime** — The `UnitOfWork` owns the single `DbContext` instance and lazily creates repositories against it, guaranteeing all repositories used within a request share the exact same tracked entity graph.

### Registration

`AddInfrastructure` (in `DependencyInjection.cs`) registers `IUnitOfWork` as scoped alongside the individual repository interfaces:

```csharp
services.AddScoped<IUnitOfWork, UnitOfWork>();
```
