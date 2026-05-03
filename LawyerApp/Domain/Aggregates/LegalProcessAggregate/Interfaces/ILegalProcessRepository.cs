using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ILegalProcessRepository
{
    // GET
    Task<LegalProcess?> GetByIdAsync(Guid id, CancellationToken cancellation);
    Task<IEnumerable<LegalProcess>> GetAllAsync(CancellationToken cancellation);

    // ADD
    Task<LegalProcess> AddAsync(LegalProcess process, CancellationToken cancellation);

    // UPDATE
    Task UpdateAsync(LegalProcess process, CancellationToken cancellation);

    // DELETE
    Task DeleteAsync(Guid id, CancellationToken cancellation);

    // Domain-Specific Methods (For Security & Filtering)
    Task<IEnumerable<LegalProcess>> GetByLawyerIdAsync(Guid lawyerId, CancellationToken cancellation);
    Task<IEnumerable<LegalProcess>> GetByClientIdAsync(Guid clientId, CancellationToken cancellation);
    Task<bool> UserHasAccessToProcessAsync(Guid userId, Guid processId, CancellationToken cancellation);
}