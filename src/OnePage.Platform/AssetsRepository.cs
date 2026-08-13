using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IAssetsRepository
{
    Task<Asset> CreateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<Asset?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class AssetsRepository(OrganizationDbContext db) : IAssetsRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task<Asset> CreateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public Task<Asset?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _db.Assets.Update(asset);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Asset>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
        _db.Assets.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<Asset>)t.Result, cancellationToken);
}
