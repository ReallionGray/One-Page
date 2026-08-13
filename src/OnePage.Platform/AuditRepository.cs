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
        var prev = await _db.AuditEvents.AsNoTracking().Where(x => x.TenantId == evt.TenantId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
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

    public Task<IReadOnlyList<AuditEvent>> ExportTenantEventsAsync(string tenantId, CancellationToken cancellationToken = default) =>
        _db.AuditEvents.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<AuditEvent>)t.Result, cancellationToken);
}
