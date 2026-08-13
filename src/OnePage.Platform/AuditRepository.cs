using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace OnePage.Platform;

public interface IAuditRepository
{
    Task AddAsync(AuditEvent evt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> ExportTenantEventsAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class AuditRepository(OrganizationDbContext db) : IAuditRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task AddAsync(AuditEvent evt, CancellationToken cancellationToken = default)
    {
        // Compute prev hash for tenant
        // SQLite cannot translate DateTimeOffset in ORDER BY; load recent tenant events and order in memory as a safe fallback for demo/dev
        var tenantEvents = await _db.AuditEvents.AsNoTracking().Where(x => x.TenantId == evt.TenantId).ToListAsync(cancellationToken);
        var prev = tenantEvents.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        evt.GetType().GetProperty("PrevHash")!.SetValue(evt, prev?.Hash);
        // compute hash of concatenated fields
        var sb = new StringBuilder();
        sb.Append(prev?.Hash ?? string.Empty);
        sb.Append("|").Append(evt.TenantId);
        sb.Append("|").Append(evt.ActorUserId);
        sb.Append("|").Append(evt.Action);
        sb.Append("|").Append(evt.ResourceType);
        sb.Append("|").Append(evt.ResourceId ?? string.Empty);
        sb.Append("|").Append(evt.BeforeJson ?? string.Empty);
        sb.Append("|").Append(evt.AfterJson ?? string.Empty);
        sb.Append("|").Append(evt.CorrelationId);
        sb.Append("|").Append(evt.Source ?? string.Empty);
        sb.Append("|").Append(evt.UserAgent ?? string.Empty);
        sb.Append("|").Append(evt.CreatedAt.ToString("o"));
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(bytes));
        evt.GetType().GetProperty("Hash")!.SetValue(evt, hash);

        _db.AuditEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> ExportTenantEventsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.AuditEvents.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        return list.OrderBy(x => x.CreatedAt).ToList();
    }
}
