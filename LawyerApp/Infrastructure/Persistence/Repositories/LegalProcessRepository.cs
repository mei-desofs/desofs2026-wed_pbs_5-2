using LawyerApp.Domain.Aggregates.LegalProcessAggregate;

namespace LawyerApp.Infrastructure.Persistence.Repositories
{
    public class LegalProcessRepository : ILegalProcessRepository
    {
        public Task<LegalProcess> AddAsync(LegalProcess process)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LegalProcess>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LegalProcess>> GetByClientIdAsync(Guid clientId)
        {
            throw new NotImplementedException();
        }

        public Task<LegalProcess?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LegalProcess>> GetByLawyerIdAsync(Guid lawyerId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(LegalProcess process)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UserHasAccessToProcessAsync(Guid userId, Guid processId)
        {
            throw new NotImplementedException();
        }
    }
}
