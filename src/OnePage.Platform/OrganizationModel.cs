using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public enum SubscriptionPlan
{
    Free,
    Basic,
    Professional,
    Enterprise
}

public sealed class Tenant
{
    private Tenant() { }
    public Tenant(string id, string name, SubscriptionPlan plan = SubscriptionPlan.Free)
    {
        Id = Required(id, nameof(id), "Tenant ID is required.");
        Name = Required(name, nameof(name), "Tenant name is required.");
        SubscriptionPlan = plan;
    }
    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public SubscriptionPlan SubscriptionPlan { get; private set; } = SubscriptionPlan.Free;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    internal static string Required(string? value, string parameter, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new OrganizationValidationException(parameter, message) : value.Trim();
    public void Rename(string name) => Name = Required(name, nameof(name), "Tenant name is required.");
    public void UpgradeSubscription(SubscriptionPlan newPlan)
    {
        SubscriptionPlan = newPlan;
    }
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

public sealed class UserProfile
{
    private UserProfile() { }
    public UserProfile(string userId, string firstName, string lastName, string email)
    {
        UserId = Tenant.Required(userId, nameof(userId), "User ID is required.");
        FirstName = Tenant.Required(firstName, nameof(firstName), "First name is required.");
        LastName = Tenant.Required(lastName, nameof(lastName), "Last name is required.");
        Email = Tenant.Required(email, nameof(email), "Email is required.");
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    public string UserId { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? ProfileImageUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Bio { get; private set; }
    public string? TimeZone { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = Tenant.Required(firstName, nameof(firstName), "First name is required.");
        LastName = Tenant.Required(lastName, nameof(lastName), "Last name is required.");
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateEmail(string email)
    {
        Email = Tenant.Required(email, nameof(email), "Email is required.");
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProfileImage(string? imageUrl)
    {
        ProfileImageUrl = imageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(string? phoneNumber, string? jobTitle, string? bio, string? timeZone, string? preferredLanguage)
    {
        PhoneNumber = phoneNumber;
        JobTitle = jobTitle;
        Bio = bio;
        TimeZone = timeZone;
        PreferredLanguage = preferredLanguage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string? PasswordHash { get; private set; }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class EntitlementAssignment
{
    private EntitlementAssignment() { }
    public EntitlementAssignment(string id, string tenantId, string entitlementNamespace, string entitlementName, EntitlementState state, string? source = null, long? limit = null)
    {
        Id = Tenant.Required(id, nameof(id), "Entitlement assignment ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        EntitlementNamespace = Tenant.Required(entitlementNamespace, nameof(entitlementNamespace), "Entitlement namespace is required.");
        EntitlementName = Tenant.Required(entitlementName, nameof(entitlementName), "Entitlement name is required.");
        State = state;
        Source = source;
        Limit = limit;
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string EntitlementNamespace { get; private set; } = null!;
    public string EntitlementName { get; private set; } = null!;
    public EntitlementState State { get; private set; }
    public string? Source { get; private set; }
    public long? Limit { get; private set; }
    public long Usage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? EffectiveAt { get; private set; }
    
    public EntitlementKey ToEntitlementKey() => new EntitlementKey(EntitlementNamespace, EntitlementName);
    public EntitlementDefinition ToDefinition() => new EntitlementDefinition(ToEntitlementKey(), State, Source, EffectiveAt, Limit, Usage);
    
    public void SetState(EntitlementState newState)
    {
        State = newState;
    }
    
    public void IncrementUsage(long amount = 1)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Usage increment cannot be negative.");
        Usage += amount;
    }
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
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MembershipRoleAssignment> MembershipRoleAssignments => Set<MembershipRoleAssignment>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<PosSale> PosSales => Set<PosSale>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<EntitlementAssignment> EntitlementAssignments => Set<EntitlementAssignment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.Entity<Tenant>(e => { e.HasKey(x => x.Id); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.SubscriptionPlan).HasConversion<string>(); });
        ConfigureOwned<LegalEntity>(modelBuilder); ConfigureOwned<Branch>(modelBuilder); ConfigureOwned<Department>(modelBuilder);
        ConfigureOwned<Location>(modelBuilder); ConfigureOwned<CostCenter>(modelBuilder);
        modelBuilder.Entity<UserMembership>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.UserId).HasMaxLength(128).IsRequired(); });
        modelBuilder.Entity<UserProfile>(e => { e.HasKey(x => x.UserId); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.UserId).HasMaxLength(128).IsRequired(); e.Property(x => x.FirstName).HasMaxLength(256).IsRequired(); e.Property(x => x.LastName).HasMaxLength(256).IsRequired(); e.Property(x => x.Email).HasMaxLength(256).IsRequired(); e.Property(x => x.ProfileImageUrl).HasMaxLength(512); e.Property(x => x.PhoneNumber).HasMaxLength(32); e.Property(x => x.JobTitle).HasMaxLength(256); e.Property(x => x.Bio).HasMaxLength(1000); e.Property(x => x.TimeZone).HasMaxLength(64); e.Property(x => x.PreferredLanguage).HasMaxLength(16); e.Property(x => x.PasswordHash).HasMaxLength(512); });
        modelBuilder.Entity<Role>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.Description).HasMaxLength(512); });
        modelBuilder.Entity<RolePermission>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.RoleId, x.Permission }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.RoleId).HasMaxLength(128).IsRequired(); e.Property(x => x.Permission).HasMaxLength(256).IsRequired(); });
        modelBuilder.Entity<MembershipRoleAssignment>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.MembershipId, x.RoleId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.MembershipId).HasMaxLength(128).IsRequired(); e.Property(x => x.RoleId).HasMaxLength(128).IsRequired(); e.Property(x => x.AmountLimit).HasPrecision(18, 2); e.Property(x => x.Currency).HasMaxLength(16); });
        modelBuilder.Entity<AuditEvent>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.CorrelationId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.ActorUserId).HasMaxLength(128).IsRequired(); e.Property(x => x.Action).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceType).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceId).HasMaxLength(128); e.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); e.Property(x => x.PrevHash).HasMaxLength(128); e.Property(x => x.Hash).HasMaxLength(128); });
        modelBuilder.Entity<Asset>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Tag }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Tag).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.CustodianEmployeeId).HasMaxLength(128); e.Property(x => x.LocationId).HasMaxLength(128); e.Property(x => x.Status).HasMaxLength(64).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); e.Property(x => x.UpdatedAt).IsRequired(false); });
        modelBuilder.Entity<ApprovalRequest>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Status }); e.HasIndex(x => new { x.TenantId, x.WorkflowDefinitionId, x.CurrentStep }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.RequestedBy).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceType).HasMaxLength(128).IsRequired(); e.Property(x => x.ResourceId).HasMaxLength(128); e.Property(x => x.Status).HasMaxLength(32).IsRequired(); e.Property(x => x.WorkflowDefinitionId).HasMaxLength(128); e.Property(x => x.CurrentStep).IsRequired(); e.Property(x => x.UpdatedAt).IsRequired(false); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<ApprovalDecision>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.ApprovalRequestId, x.StepNumber }); e.HasIndex(x => x.ApproverUserId); e.HasIndex(x => x.CreatedAt); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.ApprovalRequestId).HasMaxLength(128).IsRequired(); e.Property(x => x.StepNumber).IsRequired(); e.Property(x => x.ApproverUserId).HasMaxLength(128).IsRequired(); e.Property(x => x.Decision).HasMaxLength(32).IsRequired(); e.Property(x => x.Comment).HasMaxLength(1000); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<WorkflowDefinition>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.ResourceType, x.IsActive }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.Description).HasMaxLength(1000); e.Property(x => x.ResourceType).HasMaxLength(128).IsRequired(); e.Property(x => x.TriggerType).HasMaxLength(32).IsRequired(); e.Property(x => x.TriggerAmount).HasPrecision(18, 2); e.Property(x => x.TriggerQuantity).HasPrecision(18, 2); e.Property(x => x.IsActive).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); e.Property(x => x.UpdatedAt).IsRequired(false); });
        modelBuilder.Entity<WorkflowStep>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.WorkflowDefinitionId, x.StepNumber }); e.HasIndex(x => x.TenantId); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.WorkflowDefinitionId).HasMaxLength(128).IsRequired(); e.Property(x => x.StepNumber).IsRequired(); e.Property(x => x.ApproverType).HasMaxLength(32).IsRequired(); e.Property(x => x.ApproverValue).HasMaxLength(256).IsRequired(); e.Property(x => x.CanSkip).IsRequired(); });
        modelBuilder.Entity<PurchaseOrder>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Supplier }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Supplier).HasMaxLength(256); e.Property(x => x.Status).HasMaxLength(32).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<InventoryItem>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Sku).HasMaxLength(128).IsRequired(); e.Property(x => x.Name).HasMaxLength(256).IsRequired(); e.Property(x => x.Quantity).HasPrecision(18, 2); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<PosSale>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.RegisterId }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.RegisterId).HasMaxLength(128); e.Property(x => x.Total).HasPrecision(18, 2); e.Property(x => x.LinesJson).HasMaxLength(4000).IsRequired(false); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<JournalEntry>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.CreatedAt }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.Reference).HasMaxLength(256); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<EntitlementAssignment>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.EntitlementNamespace, x.EntitlementName }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.EntitlementNamespace).HasMaxLength(64).IsRequired(); e.Property(x => x.EntitlementName).HasMaxLength(128).IsRequired(); e.Property(x => x.State).HasConversion<string>(); e.Property(x => x.Source).HasMaxLength(256); });
        modelBuilder.Entity<Employee>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique(); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.FirstName).HasMaxLength(256).IsRequired(); e.Property(x => x.LastName).HasMaxLength(256).IsRequired(); e.Property(x => x.Email).HasMaxLength(256).IsRequired(); e.Property(x => x.DepartmentId).HasMaxLength(128); e.Property(x => x.Position).HasMaxLength(256); e.Property(x => x.Salary).HasPrecision(18, 2); e.Property(x => x.Status).HasMaxLength(32).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); });
        modelBuilder.Entity<PayrollRecord>(e => { e.HasKey(x => x.Id); e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PeriodStart }); e.Property(x => x.Id).HasMaxLength(128); e.Property(x => x.TenantId).HasMaxLength(128).IsRequired(); e.Property(x => x.EmployeeId).HasMaxLength(128).IsRequired(); e.Property(x => x.Amount).HasPrecision(18, 2); e.Property(x => x.Currency).HasMaxLength(16).IsRequired(); e.Property(x => x.Description).HasMaxLength(512); e.Property(x => x.Status).HasMaxLength(32).IsRequired(); e.Property(x => x.CreatedAt).IsRequired(); e.Property(x => x.ProcessedBy).HasMaxLength(128); });
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
    public string? WorkflowDefinitionId { get; private set; }
    public int CurrentStep { get; private set; } = 0; // 0 = direct approval (no workflow); 1..N = workflow step index
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Associates this request with a workflow definition and sets the current step to 1
    /// (the first approver in the chain). Has no effect if a workflow is already attached.
    /// </summary>
    public void AttachWorkflow(string workflowDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(workflowDefinitionId)) return;
        if (!string.IsNullOrEmpty(WorkflowDefinitionId)) return;
        WorkflowDefinitionId = Tenant.Required(workflowDefinitionId, nameof(workflowDefinitionId), "Workflow definition ID is required.");
        CurrentStep = 1;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Advances the workflow to the next step. The request remains "pending" until
    /// all steps are exhausted, at which point <see cref="Approve"/> is used to
    /// mark it fully approved.
    /// </summary>
    public void AdvanceStep(int nextStep)
    {
        if (nextStep < 0) throw new ArgumentOutOfRangeException(nameof(nextStep), "Next step cannot be negative.");
        CurrentStep = nextStep;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the request as a final approval (used when the last workflow step is approved,
    /// or when there is no workflow and a direct approval occurs).
    /// </summary>
    public void Approve(string approverUserId, string? comment)
    {
        if (string.Equals(approverUserId, RequestedBy, StringComparison.Ordinal)) throw new ArgumentException("Self-approval is not allowed.");
        DecidedBy = Tenant.Required(approverUserId, nameof(approverUserId), "Approver user ID is required.");
        DecisionComment = comment;
        Status = "approved";
        DecidedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string approverUserId, string? comment)
    {
        if (string.Equals(approverUserId, RequestedBy, StringComparison.Ordinal)) throw new ArgumentException("Self-approval is not allowed.");
        DecidedBy = Tenant.Required(approverUserId, nameof(approverUserId), "Approver user ID is required.");
        DecisionComment = comment;
        Status = "rejected";
        DecidedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Records the decision of a single approver at a specific step in a workflow.
/// One ApprovalRequest may have zero or more ApprovalDecision records (one per step).
/// </summary>
public sealed class ApprovalDecision
{
    private ApprovalDecision() { }

    public ApprovalDecision(string id, string tenantId, string approvalRequestId, int stepNumber, string approverUserId, string decision, string? comment = null)
    {
        Id = Tenant.Required(id, nameof(id), "Approval decision ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        ApprovalRequestId = Tenant.Required(approvalRequestId, nameof(approvalRequestId), "Approval request ID is required.");
        if (stepNumber < 1) throw new ArgumentOutOfRangeException(nameof(stepNumber), "Step number must be at least 1.");
        StepNumber = stepNumber;
        ApproverUserId = Tenant.Required(approverUserId, nameof(approverUserId), "Approver user ID is required.");
        Decision = Tenant.Required(decision, nameof(decision), "Decision is required.");
        Comment = comment;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string ApprovalRequestId { get; private set; } = null!;
    public int StepNumber { get; private set; }
    public string ApproverUserId { get; private set; } = null!;
    public string Decision { get; private set; } = null!; // "approved" | "rejected"
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

/// <summary>
/// Defines a workflow configuration for a resource type (e.g., purchase_order.approve).
/// Workflows are tenant-specific and define the approval chain.
/// </summary>
public sealed class WorkflowDefinition
{
    private WorkflowDefinition() { }

    public WorkflowDefinition(string id, string tenantId, string name, string resourceType, string triggerType, decimal? triggerAmount, bool isActive, string? description = null, decimal? triggerQuantity = null)
    {
        Id = Tenant.Required(id, nameof(id), "Workflow definition ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Name = Tenant.Required(name, nameof(name), "Workflow name is required.");
        ResourceType = Tenant.Required(resourceType, nameof(resourceType), "Resource type is required.");
        TriggerType = Tenant.Required(triggerType, nameof(triggerType), "Trigger type is required.");
        TriggerAmount = triggerAmount;
        TriggerQuantity = triggerQuantity;
        IsActive = isActive;
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public string TriggerType { get; private set; } = "always"; // "always" | "amount" | "quantity"
    public decimal? TriggerAmount { get; private set; }
    public decimal? TriggerQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void UpdateDetails(string name, string? description, string triggerType, decimal? triggerAmount, bool isActive, decimal? triggerQuantity = null)
    {
        Name = Tenant.Required(name, nameof(name), "Workflow name is required.");
        Description = description;
        TriggerType = Tenant.Required(triggerType, nameof(triggerType), "Trigger type is required.");
        TriggerAmount = triggerAmount;
        TriggerQuantity = triggerQuantity;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents a single step in an approval workflow, defining who should approve at that stage.
/// </summary>
public sealed class WorkflowStep
{
    private WorkflowStep() { }

    public WorkflowStep(string id, string tenantId, string workflowDefinitionId, int stepNumber, string approverType, string approverValue, bool canSkip = false)
    {
        Id = Tenant.Required(id, nameof(id), "Workflow step ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        WorkflowDefinitionId = Tenant.Required(workflowDefinitionId, nameof(workflowDefinitionId), "Workflow definition ID is required.");
        if (stepNumber < 1) throw new ArgumentOutOfRangeException(nameof(stepNumber), "Step number must be at least 1.");
        StepNumber = stepNumber;
        ApproverType = Tenant.Required(approverType, nameof(approverType), "Approver type is required.");
        ApproverValue = Tenant.Required(approverValue, nameof(approverValue), "Approver value is required.");
        CanSkip = canSkip;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string WorkflowDefinitionId { get; private set; } = null!;
    public int StepNumber { get; private set; }
    public string ApproverType { get; private set; } = null!; // "role" | "user"
    public string ApproverValue { get; private set; } = null!; // role name or user ID
    public bool CanSkip { get; private set; } = false; // if true, the next step's approver can also approve this step
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

    public PosSale(string id, string tenantId, string? registerId, decimal total, string? linesJson = null)
    {
        Id = Tenant.Required(id, nameof(id), "Sale ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        RegisterId = registerId;
        Total = total;
        LinesJson = string.IsNullOrWhiteSpace(linesJson) ? null : linesJson;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string? RegisterId { get; private set; }
    public decimal Total { get; private set; }
    public string? LinesJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class Employee
{
    private Employee() { }

    public Employee(string id, string tenantId, string firstName, string lastName, string email, string? departmentId = null, string? position = null, decimal salary = 0, DateTimeOffset? hireDate = null)
    {
        Id = Tenant.Required(id, nameof(id), "Employee ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        FirstName = Tenant.Required(firstName, nameof(firstName), "First name is required.");
        LastName = Tenant.Required(lastName, nameof(lastName), "Last name is required.");
        Email = Tenant.Required(email, nameof(email), "Email is required.");
        DepartmentId = departmentId;
        Position = position;
        Salary = salary;
        HireDate = hireDate ?? DateTimeOffset.UtcNow;
        Status = "active";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? DepartmentId { get; private set; }
    public string? Position { get; private set; }
    public decimal Salary { get; private set; }
    public DateTimeOffset HireDate { get; private set; }
    public string Status { get; private set; } = null!; // active | inactive | on_leave
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string? firstName, string? lastName, string? email, string? departmentId, string? position, decimal? salary)
    {
        if (firstName is not null) FirstName = Tenant.Required(firstName, nameof(firstName), "First name is required.");
        if (lastName is not null) LastName = Tenant.Required(lastName, nameof(lastName), "Last name is required.");
        if (email is not null) Email = Tenant.Required(email, nameof(email), "Email is required.");
        DepartmentId = departmentId;
        Position = position;
        if (salary.HasValue) Salary = salary.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Terminate()
    {
        Status = "inactive";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class PayrollRecord
{
    private PayrollRecord() { }

    public PayrollRecord(string id, string tenantId, string employeeId, decimal amount, string currency, DateTimeOffset periodStart, DateTimeOffset periodEnd, string? description = null)
    {
        Id = Tenant.Required(id, nameof(id), "Payroll record ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        EmployeeId = Tenant.Required(employeeId, nameof(employeeId), "Employee ID is required.");
        Amount = amount;
        Currency = Tenant.Required(currency, nameof(currency), "Currency is required.");
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Description = description;
        Status = "draft";
        CreatedAt = DateTimeOffset.UtcNow;
        
        // Initialize enhanced payroll fields
        GrossAmount = amount;
        TaxAmount = 0;
        PensionAmount = 0;
    }

    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string EmployeeId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; } = null!; // draft | processed | paid | cancelled
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? ProcessedBy { get; private set; }
    
    // Enhanced payroll fields for Seamless HR features
    public decimal GrossAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal PensionAmount { get; private set; }
    public decimal NetAmount => GrossAmount - TaxAmount - PensionAmount;
    public decimal? BonusAmount { get; private set; }
    public decimal? DeductionAmount { get; private set; }
    public string? TaxCode { get; private set; }
    public string? PensionScheme { get; private set; }
    public string? PayslipUrl { get; private set; }

    public void Process(string processedBy)
    {
        ProcessedBy = Tenant.Required(processedBy, nameof(processedBy), "Processor user ID is required.");
        Status = "processed";
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void ProcessWithCalculations(string processedBy, decimal taxRate = 0.2m, decimal pensionRate = 0.05m)
    {
        ProcessedBy = Tenant.Required(processedBy, nameof(processedBy), "Processor user ID is required.");
        
        // Calculate tax and pension
        TaxAmount = GrossAmount * taxRate;
        PensionAmount = GrossAmount * pensionRate;
        Amount = NetAmount; // Update amount to net amount
        
        Status = "processed";
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void AddBonus(decimal bonusAmount)
    {
        if (bonusAmount <= 0) throw new ArgumentException("Bonus amount must be positive.");
        BonusAmount = bonusAmount;
        GrossAmount += bonusAmount;
    }

    public void AddDeduction(decimal deductionAmount)
    {
        if (deductionAmount <= 0) throw new ArgumentException("Deduction amount must be positive.");
        DeductionAmount = deductionAmount;
        GrossAmount -= deductionAmount;
    }

    public void SetTaxDetails(string taxCode, decimal taxRate)
    {
        TaxCode = taxCode;
        TaxAmount = GrossAmount * taxRate;
    }

    public void SetPensionDetails(string pensionScheme, decimal pensionRate)
    {
        PensionScheme = pensionScheme;
        PensionAmount = GrossAmount * pensionRate;
    }

    public void GeneratePayslip(string payslipUrl)
    {
        PayslipUrl = payslipUrl;
    }

    public void Pay()
    {
        if (Status != "processed") throw new InvalidOperationException("Payroll must be processed before payment.");
        Status = "paid";
    }

    public void Cancel()
    {
        Status = "cancelled";
    }
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
