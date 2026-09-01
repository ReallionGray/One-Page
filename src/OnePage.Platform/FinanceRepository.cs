using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IFinanceRepository
{
    Task<JournalEntry> CreateAsync(JournalEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntry>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class FinanceRepository(OrganizationDbContext db) : IFinanceRepository
{
    private readonly OrganizationDbContext _db = db;
    public async Task<JournalEntry> CreateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }
    public async Task<IReadOnlyList<JournalEntry>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.JournalEntries.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }
}
