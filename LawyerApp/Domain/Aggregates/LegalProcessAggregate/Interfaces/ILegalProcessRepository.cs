using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LawyerApp.Domain.Aggregates.LegalProcessAggregate.Interfaces
{
    public interface ILegalProcessRepository
    {
        // GET
        Task<LegalProcess?> GetByIdAsync(Guid id, CancellationToken cancellation = default);
        Task<IEnumerable<LegalProcess>> GetAllAsync(CancellationToken cancellation = default);

        // ADD
        Task<LegalProcess> AddAsync(LegalProcess process, CancellationToken cancellation = default);

        // UPDATE
        Task UpdateAsync(LegalProcess process, CancellationToken cancellation = default);

        // DELETE
        Task DeleteAsync(Guid id, CancellationToken cancellation = default);

        // Domain-Specific Methods (For Security & Filtering)
        Task<IEnumerable<LegalProcess>> GetByLawyerIdAsync(Guid lawyerId, CancellationToken cancellation = default);
        Task<IEnumerable<LegalProcess>> GetByClientIdAsync(Guid clientId, CancellationToken cancellation = default);
        Task<bool> UserHasAccessToProcessAsync(Guid userId, Guid processId, CancellationToken cancellation = default);
    }
}