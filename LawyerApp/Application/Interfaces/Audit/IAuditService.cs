using LawyerApp.Domain.Aggregates.AuditAggregate;
using LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;
using LawyerApp.Shared;

namespace LawyerApp.Application.Interfaces.Audit
{
    public interface IAuditService
    {
        Task<Result<IEnumerable<AuditLogDto>>> GetLogsByProcessAsync(Guid processId, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken);
    }

    public record AuditLogDto(int Id, DateTime TimestampUtc, Guid UserId, string UserRole, string Operation, string Resource, string ResourceId, string IpAddress, bool Success, int StatusCode, string Details);
}
