using LawyerApp.Domain.Aggregates.DocumentAggregate;

namespace LawyerApp.Infrastructure.Persistence.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        public Task<Document> AddAsync(Document document)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Document?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Document?> GetByStoredFileNameAsync(string storedFileName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Document>> GetDocumentsByProcessIdAsync(Guid processId)
        {
            throw new NotImplementedException();
        }
    }
}
