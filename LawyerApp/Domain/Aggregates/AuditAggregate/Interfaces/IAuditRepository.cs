using LawyerApp.Domain.Aggregates.AuditAggregate;

namespace LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByResourceIdAsync(string resourceId, CancellationToken cancellationToken = default);
}
