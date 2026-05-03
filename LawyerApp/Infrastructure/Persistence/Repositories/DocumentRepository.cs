using LawyerApp.Domain.Aggregates.DocumentAggregate;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace LawyerApp.Infrastructure.Persistence.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly LawyerAppDbContext _context;

        public DocumentRepository(LawyerAppDbContext context)
        {
            _context = context;
        }

        // GET

        
        public async Task<Document?> GetByIdAsync(int id, CancellationToken cancellation)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == id, cancellation);
        }

        public async Task<Document?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellation)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.StoredFileName == storedFileName, cancellation);
        }

        public async Task<IEnumerable<Document>> GetDocumentsByProcessIdAsync(Guid processId, CancellationToken cancellation)
        {
            return await _context.Documents.Where(d => d.LegalProcessId == processId).ToListAsync(cancellation);
        }

        // ADD

        public async Task<Document> AddAsync(Document document, CancellationToken cancellation)
        {
            await _context.Documents.AddAsync(document, cancellation);
            await _context.SaveChangesAsync(cancellation);
            return document;
        }

        // UPDATE

        public async Task UpdateAsync(Document document, CancellationToken cancellation)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync(cancellation);
        }

        // DELETE

        public async Task DeleteAsync(int id, CancellationToken cancellation)
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == id, cancellation);
            if (doc != null)
            {
                _context.Documents.Remove(doc);
                await _context.SaveChangesAsync(cancellation);
            }
        }
    }
}
