using LawyerApp.Domain.Aggregates.AuditAggregate;
using LawyerApp.Domain.Aggregates.AuditAggregate.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LawyerApp.Infrastructure.Persistence.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly LawyerAppDbContext _context;

    public AuditRepository(LawyerAppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _context.Set<AuditLog>().AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.TimestampUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByResourceIdAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .Where(x => x.ResourceId == resourceId)
            .OrderByDescending(x => x.TimestampUtc)
            .ToListAsync(cancellationToken);
    }
}
