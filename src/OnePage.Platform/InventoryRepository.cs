using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IInventoryRepository
{
    Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetAsync(string id, CancellationToken cancellationToken = default);
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
    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _db.InventoryItems.Update(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
