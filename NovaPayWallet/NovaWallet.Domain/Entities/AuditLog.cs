using NovaWallet.Domain.Enums;

namespace NovaWallet.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string EntityName { get; private set; }
    public AuditOperation Operation { get; private set; }
    public string? Details { get; private set; }
    public Guid? PerformedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AuditLog()  
    {
        EntityName = string.Empty;
    }

    public AuditLog(
        Guid id,
        Guid entityId,
        string entityName,
        AuditOperation operation,
        string? details = null,
        Guid? performedByUserId = null)
    {
        Id = id;
        EntityId = entityId;
        EntityName = entityName;
        Operation = operation;
        Details = details;
        PerformedByUserId = performedByUserId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
