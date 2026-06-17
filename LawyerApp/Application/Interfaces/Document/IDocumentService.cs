using LawyerApp.Application.DTOS.Document;
using LawyerApp.Shared;
using Microsoft.AspNetCore.Http;

namespace LawyerApp.Application.Interfaces.Document;

public interface IDocumentService
{
    Task<Result<DocumentDto>> UploadDocumentAsync(Guid processId, IFormFile file, string category, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default);
    Task<Result<(byte[] content, string fileName, string contentType)>> DownloadDocumentAsync(int documentId, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<DocumentDto>>> GetDocumentsByProcessAsync(Guid processId, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default);
}
