using LawyerApp.Domain.Aggregates.DocumentAggregate;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDocumentRepository
{
    // GET
    Task<Document?> GetByIdAsync(int id, CancellationToken cancellation);
    Task<IEnumerable<Document>> GetDocumentsByProcessIdAsync(Guid processId, CancellationToken cancellation);
    Task<Document?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellation);

    // ADD
    Task<Document> AddAsync(Document document, CancellationToken cancellation);

    // UPDATE
    Task UpdateAsync(Document document, CancellationToken cancellation);

    // DELETE
    Task DeleteAsync(int id, CancellationToken cancellation);
}