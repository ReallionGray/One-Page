using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public readonly record struct PermissionKey
{
    public PermissionKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('.', StringComparison.Ordinal))
            throw new ArgumentException("Permission must be a non-empty action identifier such as 'employee.view'.", nameof(value));
        Value = value.Trim();
    }

    public string Value { get; }
    public override string ToString() => Value;
    public static implicit operator PermissionKey(string value) => new(value);
}

public static class PermissionCatalog
{
    public static readonly PermissionKey EmployeeView = new("employee.view");
    public static readonly PermissionKey PayrollRun = new("payroll.run");
    public static readonly PermissionKey ReportExport = new("report.export");

    // Asset permissions
    public static readonly PermissionKey AssetCreate = new("asset.create");
    public static readonly PermissionKey AssetView = new("asset.view");
    public static readonly PermissionKey AssetAssign = new("asset.assign");
    public static readonly PermissionKey AssetTransfer = new("asset.transfer");
    public static readonly PermissionKey AssetDispose = new("asset.dispose");
    public static readonly PermissionKey ApprovalReview = new("approval.review");

    // Procurement
    public static readonly PermissionKey PurchaseOrderCreate = new("purchase_order.create");
    public static readonly PermissionKey PurchaseOrderApprove = new("purchase_order.approve");

    // Inventory
    public static readonly PermissionKey InventoryAdjust = new("inventory.adjust");
    public static readonly PermissionKey InventoryView = new("inventory.view");

    // POS
    public static readonly PermissionKey PosSaleCreate = new("pos.sale.create");

    // Finance
    public static readonly PermissionKey FinanceJournalExport = new("finance.journal.export");

    // Reporting
    public static readonly PermissionKey ReportRun = new("report.run");

    public static PermissionKey Create(string action) => new(action);
}

public enum AuthorizationDenialReason
{
    MissingMembership,
    InactiveMembership,
    MissingPermission,
    ScopeMismatch,
    AmountLimitExceeded
}

public sealed record AuthorizationScope(
    string? LegalEntityId = null,
    string? BranchId = null,
    string? DepartmentId = null,
    string? LocationId = null,
    IReadOnlySet<string>? ManagerChainUserIds = null);

public sealed record AuthorizationRequest(
    TenantContext Context,
    PermissionKey Permission,
    AuthorizationScope? Scope = null,
    decimal? Amount = null,
    string? Currency = null);

public sealed record AuthorizationDecision(bool Allowed, AuthorizationDenialReason? DenialReason = null)
{
    public static AuthorizationDecision Allow() => new(true);
    public static AuthorizationDecision Deny(AuthorizationDenialReason reason) => new(false, reason);
}

public sealed class Role
{
    private Role() { }
    public Role(string id, string tenantId, string name)
    {
        Id = Tenant.Required(id, nameof(id), "Role ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Name = Tenant.Required(name, nameof(name), "Role name is required.");
    }
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
}

public sealed class RolePermission
{
    private RolePermission() { }
    public RolePermission(string id, string tenantId, string roleId, PermissionKey permission)
    {
        Id = Tenant.Required(id, nameof(id), "Role permission ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        RoleId = Tenant.Required(roleId, nameof(roleId), "Role ID is required.");
        Permission = permission.ToString();
    }
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string RoleId { get; private set; } = null!;
    public string Permission { get; private set; } = null!;
}

public sealed class MembershipRoleAssignment
{
    private MembershipRoleAssignment() { }
    public MembershipRoleAssignment(string id, string tenantId, string membershipId, string roleId,
        string? legalEntityId = null, string? branchId = null, string? departmentId = null,
        string? locationId = null, string? managerUserId = null, decimal? amountLimit = null, string? currency = null)
    {
        if (amountLimit is < 0) throw new ArgumentOutOfRangeException(nameof(amountLimit), "Amount limit cannot be negative.");
        Id = Tenant.Required(id, nameof(id), "Role assignment ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        MembershipId = Tenant.Required(membershipId, nameof(membershipId), "Membership ID is required.");
        RoleId = Tenant.Required(roleId, nameof(roleId), "Role ID is required.");
        LegalEntityId = Optional(legalEntityId); BranchId = Optional(branchId); DepartmentId = Optional(departmentId);
        LocationId = Optional(locationId); ManagerUserId = Optional(managerUserId); AmountLimit = amountLimit;
        Currency = Optional(currency);
    }
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string MembershipId { get; private set; } = null!;
    public string RoleId { get; private set; } = null!;
    public string? LegalEntityId { get; private set; }
    public string? BranchId { get; private set; }
    public string? DepartmentId { get; private set; }
    public string? LocationId { get; private set; }
    public string? ManagerUserId { get; private set; }
    public decimal? AmountLimit { get; private set; }
    public string? Currency { get; private set; }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface IAuthorizationRepository
{
    Task<UserMembership?> GetMembershipForUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Role Role, IReadOnlyList<RolePermission> Permissions, MembershipRoleAssignment Assignment)>>
        GetAssignmentsAsync(UserMembership membership, CancellationToken cancellationToken = default);
}

public sealed class AuthorizationRepository(OrganizationDbContext db) : IAuthorizationRepository
{
    public Task<UserMembership?> GetMembershipForUserAsync(string tenantIdentifier, string userIdentifier, CancellationToken cancellationToken = default) =>
        db.UserMemberships.AsNoTracking().SingleOrDefaultAsync(membership => membership.TenantId == tenantIdentifier && membership.UserId == userIdentifier, cancellationToken);

    public async Task<IReadOnlyList<(Role Role, IReadOnlyList<RolePermission> Permissions, MembershipRoleAssignment Assignment)>>
        GetAssignmentsAsync(UserMembership membership, CancellationToken cancellationToken = default)
    {
        var assignments = await db.MembershipRoleAssignments.AsNoTracking().Where(x => x.TenantId == membership.TenantId && x.MembershipId == membership.Id).ToListAsync(cancellationToken);
        var roleIds = assignments.Select(x => x.RoleId).Distinct().ToArray();
        var roles = await db.Roles.AsNoTracking().Where(x => x.TenantId == membership.TenantId && roleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var permissions = await db.RolePermissions.AsNoTracking().Where(x => x.TenantId == membership.TenantId && roleIds.Contains(x.RoleId)).ToListAsync(cancellationToken);
        return assignments.Where(x => roles.ContainsKey(x.RoleId)).Select(x => (roles[x.RoleId], (IReadOnlyList<RolePermission>)permissions.Where(p => p.RoleId == x.RoleId).ToList(), x)).ToList();
    }
}

public interface IAuthorizationEvaluator
{
    Task<AuthorizationDecision> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
}

public sealed class ScopedAuthorizationEvaluator(IAuthorizationRepository repository) : IAuthorizationEvaluator
{
    public async Task<AuthorizationDecision> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var membership = await repository.GetMembershipForUserAsync(request.Context.TenantId, request.Context.UserId, cancellationToken);
        if (membership is null) return AuthorizationDecision.Deny(AuthorizationDenialReason.MissingMembership);
        if (!membership.IsActive) return AuthorizationDecision.Deny(AuthorizationDenialReason.InactiveMembership);

        var assignments = await repository.GetAssignmentsAsync(membership, cancellationToken);
        var permissionAssignments = assignments.Where(x => x.Permissions.Any(p => p.Permission == request.Permission.ToString())).ToList();
        if (permissionAssignments.Count == 0) return AuthorizationDecision.Deny(AuthorizationDenialReason.MissingPermission);
        var scopedMatches = permissionAssignments.Where(x => ScopeMatches(x.Assignment, request.Scope)).ToList();
        if (scopedMatches.Count == 0) return AuthorizationDecision.Deny(AuthorizationDenialReason.ScopeMismatch);
        if (request.Amount is < 0) throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount cannot be negative.");
        if (request.Amount is not null && scopedMatches.All(x => x.Assignment.AmountLimit is not null && request.Amount > x.Assignment.AmountLimit))
            return AuthorizationDecision.Deny(AuthorizationDenialReason.AmountLimitExceeded);
        return AuthorizationDecision.Allow();
    }

    private static bool ScopeMatches(MembershipRoleAssignment assignment, AuthorizationScope? scope)
    {
        if (scope is null) return assignment.LegalEntityId is null && assignment.BranchId is null && assignment.DepartmentId is null && assignment.LocationId is null && assignment.ManagerUserId is null;
        return Matches(assignment.LegalEntityId, scope.LegalEntityId) && Matches(assignment.BranchId, scope.BranchId) && Matches(assignment.DepartmentId, scope.DepartmentId) && Matches(assignment.LocationId, scope.LocationId) && (assignment.ManagerUserId is null || scope.ManagerChainUserIds?.Contains(assignment.ManagerUserId) == true);
    }
    private static bool Matches(string? assigned, string? requested) => assigned is null || string.Equals(assigned, requested, StringComparison.Ordinal);
}
