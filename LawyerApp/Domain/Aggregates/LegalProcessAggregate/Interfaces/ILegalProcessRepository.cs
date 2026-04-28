using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILegalProcessRepository
{
    // Standard CRUD
    Task<LegalProcess?> GetByIdAsync(Guid id);
    Task<IEnumerable<LegalProcess>> GetAllAsync();
    Task<LegalProcess> AddAsync(LegalProcess process);
    Task UpdateAsync(LegalProcess process);
    Task DeleteAsync(Guid id);

    // Domain-Specific Methods (For Security & Filtering)
    Task<IEnumerable<LegalProcess>> GetByLawyerIdAsync(Guid lawyerId);
    Task<IEnumerable<LegalProcess>> GetByClientIdAsync(Guid clientId);
    Task<bool> UserHasAccessToProcessAsync(Guid userId, Guid processId);
}