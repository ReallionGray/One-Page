using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface ITenantContextAccessor { TenantContext? Current { get; } }
public sealed class TenantContextAccessor : ITenantContextAccessor
{
    public TenantContext? Current { get; set; }
}

public interface ITenantRepository
{
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task<Tenant?> GetAsync(string id, CancellationToken cancellationToken = default);
}

public interface IOrganizationRepository
{
    Task<T> CreateAsync<T>(T record, CancellationToken cancellationToken = default) where T : TenantOwnedRecord;
    Task<T?> GetAsync<T>(string id, CancellationToken cancellationToken = default) where T : TenantOwnedRecord;
    Task<T> UpdateAsync<T>(T record, CancellationToken cancellationToken = default) where T : TenantOwnedRecord;
    Task<UserMembership> CreateMembershipAsync(UserMembership membership, CancellationToken cancellationToken = default);
    Task<UserMembership?> GetMembershipAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class TenantRepository(OrganizationDbContext db) : ITenantRepository
{
    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    { db.Tenants.Add(tenant); await db.SaveChangesAsync(cancellationToken); return tenant; }
    public Task<Tenant?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Tenant.Required(id, nameof(id), "Tenant ID is required."), cancellationToken);
}

public sealed class OrganizationRepository(OrganizationDbContext db, ITenantContextAccessor context) : IOrganizationRepository
{
    private string TenantId => context.Current?.TenantId ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
    public async Task<T> CreateAsync<T>(T record, CancellationToken cancellationToken = default) where T : TenantOwnedRecord
    { EnsureTenant(record.TenantId); db.Set<T>().Add(record); await db.SaveChangesAsync(cancellationToken); return record; }
    public Task<T?> GetAsync<T>(string id, CancellationToken cancellationToken = default) where T : TenantOwnedRecord =>
        db.Set<T>().SingleOrDefaultAsync(x => x.Id == Tenant.Required(id, nameof(id), "Record ID is required.") && x.TenantId == TenantId, cancellationToken);
    public async Task<T> UpdateAsync<T>(T record, CancellationToken cancellationToken = default) where T : TenantOwnedRecord
    { EnsureTenant(record.TenantId); db.Entry(record).State = EntityState.Modified; await db.SaveChangesAsync(cancellationToken); return record; }
    public async Task<UserMembership> CreateMembershipAsync(UserMembership membership, CancellationToken cancellationToken = default)
    { EnsureTenant(membership.TenantId); db.UserMemberships.Add(membership); await db.SaveChangesAsync(cancellationToken); return membership; }
    public Task<UserMembership?> GetMembershipAsync(string id, CancellationToken cancellationToken = default) =>
        db.UserMemberships.SingleOrDefaultAsync(x => x.Id == Tenant.Required(id, nameof(id), "Membership ID is required.") && x.TenantId == TenantId, cancellationToken);
    private void EnsureTenant(string recordTenantId) { if (!string.Equals(recordTenantId, TenantId, StringComparison.Ordinal)) throw new TenantContextValidationException("tenantId", "Record tenant does not match the current tenant context."); }
}

public static class OrganizationPersistence
{
    public static async Task InitializeAsync(OrganizationDbContext db, CancellationToken cancellationToken = default) => await db.Database.EnsureCreatedAsync(cancellationToken);
}
