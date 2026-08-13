using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IProcurementRepository
{
    Task<PurchaseOrder> CreateAsync(PurchaseOrder po, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseOrder po, CancellationToken cancellationToken = default);
}

public sealed class ProcurementRepository(OrganizationDbContext db) : IProcurementRepository
{
    private readonly OrganizationDbContext _db = db;
    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder po, CancellationToken cancellationToken = default)
    {
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(cancellationToken);
        return po;
    }
    public Task<PurchaseOrder?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task UpdateAsync(PurchaseOrder po, CancellationToken cancellationToken = default)
    {
        _db.PurchaseOrders.Update(po);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
