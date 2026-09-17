using NovaWallet.Domain.Entities;

namespace NovaWallet.Infrastructure.Repositories;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default);
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
