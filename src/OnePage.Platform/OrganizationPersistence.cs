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
    Task<IReadOnlyList<Tenant>> ListByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyList<Tenant>> ListByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<Tenant>();
        return await db.Tenants.AsNoTracking().Where(t => idList.Contains(t.Id)).ToListAsync(cancellationToken);
    }
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
    public static async Task InitializeAsync(OrganizationDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await ApplySchemaPatchesAsync(db, cancellationToken);
    }

    private static async Task ApplySchemaPatchesAsync(OrganizationDbContext db, CancellationToken cancellationToken)
    {
        // Check if using SQLite by examining the connection string
        var connectionString = db.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
            return;

        await EnsureColumnAsync(db, "PosSales", "LinesJson", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(db, "Tenants", "SubscriptionPlan", "TEXT NOT NULL DEFAULT 'Free'", cancellationToken);
        await EnsureColumnAsync(db, "UserProfiles", "PasswordHash", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(db, "Roles", "Description", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(db, "ApprovalRequests", "WorkflowDefinitionId", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(db, "ApprovalRequests", "CurrentStep", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(db, "ApprovalRequests", "UpdatedAt", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(db, "WorkflowDefinitions", "TriggerQuantity", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(db, "WorkflowSteps", "CanSkip", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
    }

    private static async Task EnsureColumnAsync(OrganizationDbContext db, string table, string column, string definition, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var cmdCheck = connection.CreateCommand();
            cmdCheck.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'";
            var exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync(cancellationToken)) > 0;
            if (!exists)
            {
#pragma warning disable EF1002 // SQL injection warning - table/column names are hardcoded strings from trusted source
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition}", cancellationToken);
#pragma warning restore EF1002
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }
}
