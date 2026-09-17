using Microsoft.EntityFrameworkCore;
using NovaWallet.Domain.Entities;
using NovaWallet.Infrastructure.Persistence;

namespace NovaWallet.Infrastructure.Repositories;

public class WalletRepository(NovaWalletDbContext _dbContext) : IWalletRepository
{
    //private readonly NovaWalletDbContext _dbContext = dbContext;

    public Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public Task<Wallet?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId, cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _dbContext.Wallets.AddAsync(wallet, cancellationToken);
    }

    public void Update(Wallet wallet)
    {
        _dbContext.Wallets.Update(wallet);
    }
}
