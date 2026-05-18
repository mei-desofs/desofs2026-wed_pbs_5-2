using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace LawyerApp.Infrastructure.Persistence.Repositories
{
    public class LegalProcessRepository : ILegalProcessRepository
    {
        private readonly LawyerAppDbContext _context;

        public LegalProcessRepository(LawyerAppDbContext context)
        {
            _context = context;
        }
        // GET
        public async Task<IEnumerable<LegalProcess>> GetAllAsync(CancellationToken cancellation)
        {
            return await _context.LegalProcesses.ToListAsync(cancellation);
        }

        public async Task<LegalProcess?> GetByIdAsync(Guid id, CancellationToken cancellation)
        {
            return await _context.LegalProcesses.FirstOrDefaultAsync(p => p.ProcessId == id, cancellation);
        }

        public async Task<IEnumerable<LegalProcess>> GetByLawyerIdAsync(Guid lawyerId, CancellationToken cancellation)
        {
            return await _context.LegalProcesses.Where(p => p.LawyerId == lawyerId).ToListAsync(cancellation);
        }

        public async Task<IEnumerable<LegalProcess>> GetByClientIdAsync(Guid clientId, CancellationToken cancellation)
        {
            return await _context.LegalProcesses.Where(p => p.ClientId == clientId).ToListAsync(cancellation);
        }

        public async Task<bool> UserHasAccessToProcessAsync(Guid userId, Guid processId, CancellationToken cancellation)
        {
            var proc = await _context.LegalProcesses.FirstOrDefaultAsync(p => p.ProcessId == processId, cancellation);
            if (proc == null) return false;
            return proc.ClientId == userId || proc.LawyerId == userId;
        }

        // ADD
        public async Task<LegalProcess> AddAsync(LegalProcess process, CancellationToken cancellation)
        {
            await _context.LegalProcesses.AddAsync(process, cancellation);
            await _context.SaveChangesAsync(cancellation);
            return process;
        }

        // UPDATE
        public async Task UpdateAsync(LegalProcess process, CancellationToken cancellation)
        {
            _context.LegalProcesses.Update(process);
            await _context.SaveChangesAsync(cancellation);
        }

        // DELETE
        public async Task DeleteAsync(Guid id, CancellationToken cancellation)
        {
            var proc = await _context.LegalProcesses.FirstOrDefaultAsync(p => p.ProcessId == id, cancellation);
            if (proc != null)
            {
                _context.LegalProcesses.Remove(proc);
                await _context.SaveChangesAsync(cancellation);
            }
        }
    }
}
