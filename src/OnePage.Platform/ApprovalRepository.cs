using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IApprovalRepository
{
    Task<ApprovalRequest> CreateAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalRequest?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class ApprovalRepository(OrganizationDbContext db) : IApprovalRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task<ApprovalRequest> CreateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<ApprovalRequest?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.ApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _db.ApprovalRequests.Update(request);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ApprovalRequests.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == "pending").ToListAsync(cancellationToken);
        return list.OrderBy(x => x.CreatedAt).ToList();
    }
}
