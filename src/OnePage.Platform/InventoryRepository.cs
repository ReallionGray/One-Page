using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IInventoryRepository
{
    Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetBySkuAsync(string tenantId, string sku, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetBySkuForUpdateAsync(string tenantId, string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
}

public sealed class InventoryRepository(OrganizationDbContext db) : IInventoryRepository
{
    private readonly OrganizationDbContext _db = db;
    public async Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item;
    }
    public Task<InventoryItem?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InventoryItem?> GetBySkuAsync(string tenantId, string sku, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Sku == sku, cancellationToken);

    public Task<InventoryItem?> GetBySkuForUpdateAsync(string tenantId, string sku, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Sku == sku, cancellationToken);

    public Task<IReadOnlyList<InventoryItem>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.Sku).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<InventoryItem>)t.Result, cancellationToken);

    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(item).State == EntityState.Detached)
            _db.InventoryItems.Update(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
