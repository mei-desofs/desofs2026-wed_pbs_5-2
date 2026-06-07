using LawyerApp.Application.DTOS.LegalProcess;
using LawyerApp.Application.Interfaces.LegalProcess;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.AuditAggregate;
using LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;
using LawyerApp.Domain.Shared;
using LawyerApp.Shared;
using System.IO;

namespace LawyerApp.Application.Services.LegalProcess;

public class LegalProcessService : ILegalProcessService
{
    private readonly ILegalProcessRepository _processRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly string _baseStoragePath;

    public LegalProcessService(ILegalProcessRepository processRepository, IUserRepository userRepository, IAuditRepository auditRepository)
    {
        _processRepository = processRepository;
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _baseStoragePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "LawyerAppData", "Processes"));
    }

    public async Task<Result<LegalProcessDto>> CreateProcessAsync(CreateLegalProcessDto dto, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default)
    {
        // RF01: Only Lawyer can create processes
        if (currentUserRole != Roles.Lawyer.ToString())
        {
            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Create Process", "LegalProcess", "N/A", ipAddress, false, 403, "Forbidden: Only lawyers can create processes"), cancellationToken);
            return Result<LegalProcessDto>.Failure(403, "Only lawyers can create processes.");
        }

        var process = new LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess(dto.Title, dto.Description, dto.LawyerId, dto.ClientId);

        try 
        {
            // RF03: Create directory structure
            var processPath = Path.Combine(_baseStoragePath, process.ProcessId.ToString());

            // Security: Use GetFullPath to prevent traversal (though GUID prevents it mostly)
            var fullPath = Path.GetFullPath(processPath);
            if (!fullPath.StartsWith(Path.GetFullPath(_baseStoragePath)))
            {
                 throw new UnauthorizedAccessException("Potential path traversal detected.");
            }

            Directory.CreateDirectory(Path.Combine(fullPath, "Documents", "Petitions"));
            Directory.CreateDirectory(Path.Combine(fullPath, "Documents", "Contracts"));
            Directory.CreateDirectory(Path.Combine(fullPath, "Documents", "Others"));

            await _processRepository.AddAsync(process, cancellationToken);

            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Create Process", "LegalProcess", process.ProcessId.ToString(), ipAddress, true, 201, "Process created successfully"), cancellationToken);

            return Result<LegalProcessDto>.Success(MapToDto(process));
        }
        catch (Exception ex)
        {
            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Create Process", "LegalProcess", process.ProcessId.ToString(), ipAddress, false, 500, $"Error: {ex.Message}"), cancellationToken);
            return Result<LegalProcessDto>.Failure(500, "Failed to create process and its directories.");
        }
    }

    public async Task<Result<LegalProcessDto>> GetProcessByIdAsync(Guid processId, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        var process = await _processRepository.GetByIdAsync(processId, cancellationToken);
        if (process == null) return Result<LegalProcessDto>.Failure(404, "Process not found.");

        // RBAC: Lawyer/Assistant of the process or the Client of the process
        bool hasAccess = await _processRepository.UserHasAccessToProcessAsync(currentUserId, processId, cancellationToken);
        if (!hasAccess && currentUserRole != Roles.Admin.ToString())
        {
            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "View Process", "LegalProcess", processId.ToString(), "N/A", false, 403, "Access denied"), cancellationToken);
            return Result<LegalProcessDto>.Failure(403, "Access denied.");
        }

        await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "View Process", "LegalProcess", processId.ToString(), "N/A", true, 200, "Process viewed"), cancellationToken);
        return Result<LegalProcessDto>.Success(MapToDto(process));
    }

    public async Task<Result<IEnumerable<LegalProcessDto>>> GetAllProcessesAsync(Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default)
    {
        IEnumerable<LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess> processes;

        if (currentUserRole == Roles.Lawyer.ToString())
        {
            processes = await _processRepository.GetByLawyerIdAsync(currentUserId, cancellationToken);
        }
        else if (currentUserRole == Roles.Client.ToString())
        {
            processes = await _processRepository.GetByClientIdAsync(currentUserId, cancellationToken);
        }
        else if (currentUserRole == Roles.Admin.ToString())
        {
            processes = await _processRepository.GetAllAsync(cancellationToken);
        }
        else if (currentUserRole == Roles.LegalAssistant.ToString())
        {
            var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            if (user is LegalAssistant assistant && assistant.AssignedLawyerId.HasValue)
            {
                processes = await _processRepository.GetByLawyerIdAsync(assistant.AssignedLawyerId.Value, cancellationToken);
            }
            else
            {
                processes = Enumerable.Empty<LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess>();
            }
        }
        else
        {
            processes = Enumerable.Empty<LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess>();
        }

        await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "List Processes", "LegalProcess", "All", "N/A", true, 200, $"Listed {processes.Count()} processes"), cancellationToken);
        return Result<IEnumerable<LegalProcessDto>>.Success(processes.Select(MapToDto));
    }

    private static LegalProcessDto MapToDto(LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess process)
    {
        return new LegalProcessDto(process.ProcessId, process.Title, process.Description, process.Status.ToString(), process.OpenedAt, process.LawyerId, process.ClientId);
    }
}
