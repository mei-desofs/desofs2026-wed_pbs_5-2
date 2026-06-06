using LawyerApp.Domain.Aggregates.DocumentAggregate;
using LawyerApp.Domain.Aggregates.DocumentAggregate;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LawyerApp.Domain.Aggregates.DocumentAggregate.Interfaces
{
    public interface IDocumentRepository
    {
        // GET
        Task<Document?> GetByIdAsync(int id, CancellationToken cancellation = default);
        Task<IEnumerable<Document>> GetDocumentsByProcessIdAsync(Guid processId, CancellationToken cancellation = default);
        Task<Document?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellation = default);

        // ADD
        Task<Document> AddAsync(Document document, CancellationToken cancellation = default);

        // UPDATE
        Task UpdateAsync(Document document, CancellationToken cancellation = default);

        // DELETE
        Task DeleteAsync(int id, CancellationToken cancellation = default);
    }
}