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
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<PosSale> PosSales => Set<PosSale>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

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
        modelBuilder.Entity<AuditEvent>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.CorrelationId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.ActorUserId).HasMaxLength(128).IsRequired(); e.Property(x => x.Action).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceType).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceId).HasMaxLength(128); e.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); e.Property(x => x.PrevHash).HasMaxLength(128); e.Property(x => x.Hash).HasMaxLength(128); });
        modelBuilder.Entity<Asset>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Tag }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Tag).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.CustodianEmployeeId).HasMaxLength(128); e.Property(x => x.LocationId).HasMaxLength(128); e.Property(x => x.Status).HasMaxLength(64).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); e.Property(x => x.UpdatedAt).IsRequired(false); });
        modelBuilder.Entity<ApprovalRequest>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Status }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.RequestedBy).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceType).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceId).HasMaxLength(128); e.Property(x => x.Status).HasMaxLength(32).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<PurchaseOrder>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Supplier }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Supplier).HasMaxLength(256); e.Property(x => x.Status).HasMaxLength(32).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<InventoryItem>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Sku).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.Quantity).HasPrecision(18, 2); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<PosSale>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.RegisterId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.RegisterId).HasMaxLength(128); e.Property(x => x.Total).HasPrecision(18, 2); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<JournalEntry>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.CreatedAt }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Reference).HasMaxLength(256); e.Property(x => x.CreatedAt).IsRequired(); });
    }

    private static void ConfigureOwned<T>(ModelBuilder modelBuilder) where T : TenantOwnedRecord
    {
        modelBuilder.Entity<T>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Name }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); });
    }
}

public sealed class Asset
{
    private Asset() { }

    public Asset(string id, string tenantId, string tag, string name, string? description = null, string? locationId = null, string? custodianEmployeeId = null)
    {
        Id = Tenant.Required(id, nameof(id), "Asset ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Tag = Tenant.Required(tag, nameof(tag), "Asset tag is required.");
        Name = Tenant.Required(name, nameof(name), "Asset name is required.");
        Description = description;
        LocationId = locationId;
        CustodianEmployeeId = custodianEmployeeId;
        Status = "in_service";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Tag { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LocationId { get; private set; }
    public string? CustodianEmployeeId { get; private set; }
    public string Status { get; private set; } = null!; // in_service | assigned | disposed
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void AssignToEmployee(string employeeId)
    {
        CustodianEmployeeId = Tenant.Required(employeeId, nameof(employeeId), "Employee ID is required.");
        Status = "assigned";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Transfer(string? locationId, string? newCustodianEmployeeId)
    {
        LocationId = string.IsNullOrWhiteSpace(locationId) ? null : locationId.Trim();
        CustodianEmployeeId = string.IsNullOrWhiteSpace(newCustodianEmployeeId) ? null : newCustodianEmployeeId.Trim();
        Status = "in_service";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Dispose(string actorUserId)
    {
        _ = Tenant.Required(actorUserId, nameof(actorUserId), "Actor user ID is required.");
        Status = "disposed";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class ApprovalRequest
{
    private ApprovalRequest() { }

    public ApprovalRequest(string id, string tenantId, string resourceType, string resourceId, string requestedBy, string reason)
    {
        Id = Tenant.Required(id, nameof(id), "Approval request ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        ResourceType = Tenant.Required(resourceType, nameof(resourceType), "Resource type is required.");
        ResourceId = resourceId;
        RequestedBy = Tenant.Required(requestedBy, nameof(requestedBy), "Requester user ID is required.");
        Reason = reason;
        Status = "pending";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public string? ResourceId { get; private set; }
    public string RequestedBy { get; private set; } = null!;
    public string? Reason { get; private set; }
    public string Status { get; private set; } = null!; // pending | approved | rejected
    public string? DecidedBy { get; private set; }
    public string? DecisionComment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }

    public void Approve(string approverUserId, string? comment)
    {
        if (string.Equals(approverUserId, RequestedBy, StringComparison.Ordinal)) throw new ArgumentException("Self-approval is not allowed.");
        DecidedBy = Tenant.Required(approverUserId, nameof(approverUserId), "Approver user ID is required.");
        DecisionComment = comment;
        Status = "approved";
        DecidedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string approverUserId, string? comment)
    {
        if (string.Equals(approverUserId, RequestedBy, StringComparison.Ordinal)) throw new ArgumentException("Self-approval is not allowed.");
        DecidedBy = Tenant.Required(approverUserId, nameof(approverUserId), "Approver user ID is required.");
        DecisionComment = comment;
        Status = "rejected";
        DecidedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class PurchaseOrder
{
    private PurchaseOrder() { }

    public PurchaseOrder(string id, string tenantId, string supplier, decimal totalAmount)
    {
        Id = Tenant.Required(id, nameof(id), "PO ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Supplier = Tenant.Required(supplier, nameof(supplier), "Supplier is required.");
        TotalAmount = totalAmount;
        Status = "draft";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Supplier { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; } = null!; // draft | approved | closed
    public DateTimeOffset CreatedAt { get; private set; }

    public void Approve(string approverUserId)
    {
        _ = Tenant.Required(approverUserId, nameof(approverUserId), "Approver user ID is required.");
        Status = "approved";
    }
}

public sealed class InventoryItem
{
    private InventoryItem() { }

    public InventoryItem(string id, string tenantId, string sku, string name, decimal quantity)
    {
        Id = Tenant.Required(id, nameof(id), "Inventory item ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Sku = Tenant.Required(sku, nameof(sku), "SKU is required.");
        Name = Tenant.Required(name, nameof(name), "Name is required.");
        Quantity = quantity;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void Adjust(decimal delta)
    {
        Quantity += delta;
    }
}

public sealed class PosSale
{
    private PosSale() { }

    public PosSale(string id, string tenantId, string registerId, decimal total)
    {
        Id = Tenant.Required(id, nameof(id), "Sale ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        RegisterId = registerId;
        Total = total;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string? RegisterId { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class JournalEntry
{
    private JournalEntry() { }

    public JournalEntry(string id, string tenantId, string reference)
    {
        Id = Tenant.Required(id, nameof(id), "Journal entry ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Reference = reference;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string? Reference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
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
    public string? PrevHash { get; private set; }
    public string? Hash { get; private set; }
}

public sealed class OrganizationValidationException : ArgumentException
{
    public OrganizationValidationException(string parameterName, string message) : base(message)
    {
        _ = parameterName;
    }
}
