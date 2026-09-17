using Microsoft.EntityFrameworkCore;
using NovaWallet.Domain.Entities;

namespace NovaWallet.Infrastructure.Persistence;

public class NovaWalletDbContext : DbContext
{
    public NovaWalletDbContext(DbContextOptions<NovaWalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<DailyTransferUsage> DailyTransferUsages => Set<DailyTransferUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NovaWalletDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
