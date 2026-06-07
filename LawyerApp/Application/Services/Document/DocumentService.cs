using LawyerApp.Application.DTOS.Document;
using LawyerApp.Application.Interfaces.Document;
using LawyerApp.Domain.Aggregates.DocumentAggregate;
using LawyerApp.Domain.Aggregates.DocumentAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.AuditAggregate;
using LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;
using LawyerApp.Domain.Shared;
using LawyerApp.Shared;
using Microsoft.AspNetCore.Http;
using System.IO;

using LawyerApp.Application.Interfaces.Security;

namespace LawyerApp.Application.Services.Document;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILegalProcessRepository _processRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IFileEncryptionService _encryptionService;
    private readonly string _baseStoragePath;

    public DocumentService(IDocumentRepository documentRepository, ILegalProcessRepository processRepository, IAuditRepository auditRepository, IFileEncryptionService encryptionService)
    {
        _documentRepository = documentRepository;
        _processRepository = processRepository;
        _auditRepository = auditRepository;
        _encryptionService = encryptionService;
        _baseStoragePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "LawyerAppData", "Processes"));
    }

    public async Task<Result<DocumentDto>> UploadDocumentAsync(Guid processId, IFormFile file, string category, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default)
    {
        // RF01: Lawyer or Assistant can upload
        if (currentUserRole != Roles.Lawyer.ToString() && currentUserRole != Roles.LegalAssistant.ToString())
        {
            return Result<DocumentDto>.Failure(403, "Insufficient permissions to upload documents.");
        }

        // RF02: Only PDF and DOCX
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            return Result<DocumentDto>.Failure(400, "Only PDF and DOCX files are allowed.");
        }

        // Check access to process
        bool hasAccess = await _processRepository.UserHasAccessToProcessAsync(currentUserId, processId, cancellationToken);
        if (!hasAccess)
        {
            return Result<DocumentDto>.Failure(403, "Access to process denied.");
        }

        if (!Enum.TryParse<DocCategory>(category, true, out var docCategory))
        {
            docCategory = DocCategory.Evidence;
        }

        var document = new LawyerApp.Domain.Aggregates.DocumentAggregate.Document(file.FileName, file.Length, file.ContentType, docCategory, processId);

        try
        {
            // RF03: Determine subfolder
            string subFolder = category.ToLower() switch
            {
                "petition" => Path.Combine("Documents", "Petitions"),
                "contract" => Path.Combine("Documents", "Contracts"),
                _ => Path.Combine("Documents", "Others")
            };

            var processPath = Path.GetFullPath(Path.Combine(_baseStoragePath, processId.ToString(), subFolder));
            if (!processPath.StartsWith(_baseStoragePath, StringComparison.OrdinalIgnoreCase))
            {
                return Result<DocumentDto>.Failure(400, "Invalid path.");
            }

            Directory.CreateDirectory(processPath);

            var filePath = Path.Combine(processPath, document.StoredFileName);

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await _encryptionService.EncryptStreamAsync(memoryStream, fileStream, cancellationToken);
                }
            }

            await _documentRepository.AddAsync(document, cancellationToken);
            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Upload Document", "Document", document.DocumentId.ToString(), ipAddress, true, 201, $"Uploaded {file.FileName}"), cancellationToken);

            return Result<DocumentDto>.Success(MapToDto(document));
        }
        catch (Exception ex)
        {
             await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Upload Document", "Document", "N/A", ipAddress, false, 500, $"Error: {ex.Message}"), cancellationToken);
             return Result<DocumentDto>.Failure(500, "Failed to upload document.");
        }
    }

    public async Task<Result<(byte[] content, string fileName, string contentType)>> DownloadDocumentAsync(int documentId, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null) return Result<(byte[] content, string fileName, string contentType)>.Failure(404, "Document not found.");

        // RF02: Access Check
        bool hasAccess = await _processRepository.UserHasAccessToProcessAsync(currentUserId, document.LegalProcessId, cancellationToken);
        if (!hasAccess)
        {
             await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Download Document", "Document", documentId.ToString(), ipAddress, false, 403, "Access denied"), cancellationToken);
             return Result<(byte[] content, string fileName, string contentType)>.Failure(403, "Access denied.");
        }

        try
        {
            var processPath = Path.GetFullPath(Path.Combine(_baseStoragePath, document.LegalProcessId.ToString()));
            if (!processPath.StartsWith(_baseStoragePath, StringComparison.OrdinalIgnoreCase))
            {
                return Result<(byte[] content, string fileName, string contentType)>.Failure(400, "Invalid path.");
            }

            var files = Directory.GetFiles(processPath, document.StoredFileName, SearchOption.AllDirectories);

            if (files.Length == 0) return Result<(byte[] content, string fileName, string contentType)>.Failure(404, "Physical file not found.");

            byte[] content;
            using (var fileStream = new FileStream(files[0], FileMode.Open, FileAccess.Read))
            using (var memoryStream = new MemoryStream())
            {
                await _encryptionService.DecryptStreamAsync(fileStream, memoryStream, cancellationToken);
                content = memoryStream.ToArray();
            }

            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "Download Document", "Document", documentId.ToString(), ipAddress, true, 200, $"Downloaded {document.FileName}"), cancellationToken);

            return Result<(byte[] content, string fileName, string contentType)>.Success((content, document.FileName, document.ContentType));
        }
        catch (Exception ex)
        {
             return Result<(byte[] content, string fileName, string contentType)>.Failure(500, "Failed to download.");
        }
    }

    public async Task<Result<IEnumerable<DocumentDto>>> GetDocumentsByProcessAsync(Guid processId, Guid currentUserId, string currentUserRole, string ipAddress, CancellationToken cancellationToken = default)
    {
        bool hasAccess = await _processRepository.UserHasAccessToProcessAsync(currentUserId, processId, cancellationToken);
        if (!hasAccess)
        {
            await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "List Documents", "Document", processId.ToString(), ipAddress, false, 403, "Access denied"), cancellationToken);
            return Result<IEnumerable<DocumentDto>>.Failure(403, "Access denied.");
        }

        var docs = await _documentRepository.GetDocumentsByProcessIdAsync(processId, cancellationToken);
        await _auditRepository.AddAsync(new AuditLog(currentUserId, currentUserRole, "List Documents", "Document", processId.ToString(), ipAddress, true, 200, $"Listed {docs.Count()} documents"), cancellationToken);
        return Result<IEnumerable<DocumentDto>>.Success(docs.Select(MapToDto));
    }

    private static DocumentDto MapToDto(LawyerApp.Domain.Aggregates.DocumentAggregate.Document doc)
    {
        return new DocumentDto(doc.DocumentId, doc.FileName, doc.FileSize, doc.ContentType, doc.Category.ToString(), doc.LegalProcessId, doc.UploadedAt);
    }
}
