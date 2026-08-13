using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public sealed class Tenant
{
    private Tenant() { }
    public Tenant(string id, string name)
    {
        Id = Required(id, nameof(id), "Tenant ID is required.");
        Name = Required(name, nameof(name), "Tenant name is required.");
    }
    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    internal static string Required(string? value, string parameter, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new OrganizationValidationException(parameter, message) : value.Trim();
    public void Rename(string name) => Name = Required(name, nameof(name), "Tenant name is required.");
}

public abstract class TenantOwnedRecord
{
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    protected TenantOwnedRecord() { }
    protected TenantOwnedRecord(string id, string tenantId, string name)
    {
        Id = Tenant.Required(id, nameof(id), "Record ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Name = Tenant.Required(name, nameof(name), "Record name is required.");
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public void Rename(string name) => Name = Tenant.Required(name, nameof(name), "Record name is required.");
}

public sealed class LegalEntity : TenantOwnedRecord { private LegalEntity() { } public LegalEntity(string id, string tenantId, string name) : base(id, tenantId, name) { } }
public sealed class Branch : TenantOwnedRecord { private Branch() { } public Branch(string id, string tenantId, string name) : base(id, tenantId, name) { } }
public sealed class Department : TenantOwnedRecord { private Department() { } public Department(string id, string tenantId, string name) : base(id, tenantId, name) { } }
public sealed class Location : TenantOwnedRecord { private Location() { } public Location(string id, string tenantId, string name) : base(id, tenantId, name) { } }
public sealed class CostCenter : TenantOwnedRecord { private CostCenter() { } public CostCenter(string id, string tenantId, string name) : base(id, tenantId, name) { } }

public sealed class UserMembership
{
    private UserMembership() { }
    public UserMembership(string id, string tenantId, string userId)
    {
        Id = Tenant.Required(id, nameof(id), "Membership ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        UserId = Tenant.Required(userId, nameof(userId), "User ID is required.");
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public void SetActive(bool active) => IsActive = active;
}

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<UserMembership> UserMemberships => Set<UserMembership>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MembershipRoleAssignment> MembershipRoleAssignments => Set<MembershipRoleAssignment>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.Entity<Tenant>(e => { e.HasKey(x => x.Id); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); });
        ConfigureOwned<LegalEntity>(modelBuilder); ConfigureOwned<Branch>(modelBuilder); ConfigureOwned<Department>(modelBuilder);
        ConfigureOwned<Location>(modelBuilder); ConfigureOwned<CostCenter>(modelBuilder);
        modelBuilder.Entity<UserMembership>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.UserId).HasMaxLength(128).IsRequired(); });
        modelBuilder.Entity<Role>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); });
        modelBuilder.Entity<RolePermission>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.RoleId, x.Permission }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.RoleId).HasMaxLength(128).IsRequired(); e.Property(x => x.Permission).HasMaxLength(256).IsRequired(); });
        modelBuilder.Entity<MembershipRoleAssignment>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.MembershipId, x.RoleId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.MembershipId).HasMaxLength(128).IsRequired(); e.Property(x => x.RoleId).HasMaxLength(128).IsRequired(); e.Property(x => x.AmountLimit).HasPrecision(18, 2); e.Property(x => x.Currency).HasMaxLength(16); });
        modelBuilder.Entity<AuditEvent>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.CorrelationId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.ActorUserId).HasMaxLength(128).IsRequired(); e.Property(x => x.Action).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceType).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceId).HasMaxLength(128); e.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); });
    }

    private static void ConfigureOwned<T>(ModelBuilder modelBuilder) where T : TenantOwnedRecord
    {
        modelBuilder.Entity<T>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Name }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); });
    }
}

public sealed class AuditEvent
{
    private AuditEvent() { }

    public AuditEvent(string id, string tenantId, string actorUserId, string action, string resourceType, string? resourceId, string? beforeJson, string? afterJson, string correlationId, string? source, string? userAgent)
    {
        Id = Tenant.Required(id, nameof(id), "AuditEvent ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        ActorUserId = Tenant.Required(actorUserId, nameof(actorUserId), "Actor user ID is required.");
        Action = Tenant.Required(action, nameof(action), "Action is required.");
        ResourceType = Tenant.Required(resourceType, nameof(resourceType), "Resource type is required.");
        ResourceId = resourceId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        CorrelationId = Tenant.Required(correlationId, nameof(correlationId), "Correlation ID is required.");
        Source = source;
        UserAgent = userAgent;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string ActorUserId { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public string? ResourceId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public string? Source { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class OrganizationValidationException : ArgumentException
{
    public OrganizationValidationException(string parameterName, string message) : base(message)
    {
        _ = parameterName;
    }
}
