using Microsoft.EntityFrameworkCore;
using NovaWallet.Domain.Entities;
using NovaWallet.Infrastructure.Persistence;

namespace NovaWallet.Infrastructure.Repositories;

public class AuditLogRepository(NovaWalletDbContext _dbContext) : IAuditLogRepository
{
     

    public async Task<IReadOnlyList<AuditLog>> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
        
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
