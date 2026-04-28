using LawyerApp.Domain.Aggregates.DocumentAggregate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDocumentRepository
{
    // Standard CRUD
    Task<Document?> GetByIdAsync(Guid id);
    Task<Document> AddAsync(Document document);
    Task DeleteAsync(Guid id);

    // Domain-Specific Methods
    Task<IEnumerable<Document>> GetDocumentsByProcessIdAsync(Guid processId);

    // Vital for physical file retrieval (maps the GUID filename back to the real name)
    Task<Document?> GetByStoredFileNameAsync(string storedFileName);
}