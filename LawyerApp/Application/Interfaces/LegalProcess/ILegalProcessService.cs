using LawyerApp.Application.DTOS.LegalProcess;
using LawyerApp.Shared;

namespace LawyerApp.Application.Interfaces.LegalProcess;

public interface ILegalProcessService
{
    Task<Result<LegalProcessDto>> CreateProcessAsync(CreateLegalProcessDto dto, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default);
    Task<Result<LegalProcessDto>> GetProcessByIdAsync(Guid processId, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<LegalProcessDto>>> GetAllProcessesAsync(Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
}
