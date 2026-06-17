using LawyerApp.Domain.Aggregates.DocumentAggregate;

namespace LawyerApp.Application.DTOS.Document;

public record DocumentDto(int DocumentId, string FileName, long FileSize, string ContentType, string Category, Guid LegalProcessId, DateTime UploadedAt);
