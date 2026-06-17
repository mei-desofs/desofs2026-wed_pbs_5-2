using LawyerApp.Application.Interfaces.Audit;
using LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate.Interfaces;
using LawyerApp.Domain.Shared;
using LawyerApp.Shared;

namespace LawyerApp.Application.Services.Audit
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILegalProcessRepository _processRepository;

        public AuditService(IAuditRepository auditRepository, ILegalProcessRepository processRepository)
        {
            _auditRepository = auditRepository;
            _processRepository = processRepository;
        }

        public async Task<Result<IEnumerable<AuditLogDto>>> GetLogsByProcessAsync(Guid processId, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken)
        {
            // RBAC Check
            bool hasAccess = await _processRepository.UserHasAccessToProcessAsync(currentUserId, processId, cancellationToken);
            if (!hasAccess && currentUserRole != Roles.Admin.ToString())
            {
                return Result<IEnumerable<AuditLogDto>>.Failure(403, "Access denied to audit logs.");
            }

            var logs = await _auditRepository.GetByResourceIdAsync(processId.ToString(), cancellationToken);

            var result = logs.Select(l => new AuditLogDto(
                l.Id, l.TimestampUtc, l.UserId, l.UserRole, l.Operation, l.Resource, l.ResourceId, l.IpAddress, l.Success, l.StatusCode, l.Details
            ));

            return Result<IEnumerable<AuditLogDto>>.Success(result);
        }
    }
}
