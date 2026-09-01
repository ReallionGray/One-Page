using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IPosRepository
{
    Task<PosSale> CreateAsync(PosSale sale, CancellationToken cancellationToken = default);
    Task<PosSale?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosSale>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
}

public sealed class PosRepository(OrganizationDbContext db) : IPosRepository
{
    private readonly OrganizationDbContext _db = db;
    public async Task<PosSale> CreateAsync(PosSale sale, CancellationToken cancellationToken = default)
    {
        _db.PosSales.Add(sale);
        await _db.SaveChangesAsync(cancellationToken);
        return sale;
    }
    public Task<PosSale?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.PosSales.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PosSale>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.PosSales.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        return list.OrderByDescending(x => x.CreatedAt).ToList();
    }
}
