using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IAuditRepository
{
    Task AddAsync(AuditEvent evt, CancellationToken cancellationToken = default);
}

public sealed class AuditRepository(OrganizationDbContext db) : IAuditRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task AddAsync(AuditEvent evt, CancellationToken cancellationToken = default)
    {
        _db.AuditEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
