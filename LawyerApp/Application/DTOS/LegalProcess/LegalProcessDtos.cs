namespace LawyerApp.Application.DTOS.LegalProcess;
using System.ComponentModel.DataAnnotations;

public record CreateLegalProcessDto(
    [Required] [StringLength(200)] string Title, 
    [Required] [StringLength(2000)] string Description, 
    [Required] Guid LawyerId, 
    [Required] Guid ClientId);

public record LegalProcessDto(Guid ProcessId, string Title, string Description, string Status, DateTime OpenedAt, Guid LawyerId, Guid ClientId);
