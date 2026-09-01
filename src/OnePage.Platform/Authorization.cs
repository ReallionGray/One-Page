using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    public static readonly PermissionKey EmployeeCreate = new("employee.create");
    public static readonly PermissionKey EmployeeUpdate = new("employee.update");
    public static readonly PermissionKey EmployeeTerminate = new("employee.terminate");
    public static readonly PermissionKey LeaveRequest = new("leave.request");
    public static readonly PermissionKey LeaveApprove = new("leave.approve");
    public static readonly PermissionKey DisciplinaryManage = new("disciplinary.manage");
    public static readonly PermissionKey RecruitmentManage = new("recruitment.manage");
    public static readonly PermissionKey PerformanceManage = new("performance.manage");
    public static readonly PermissionKey AttendanceManage = new("attendance.manage");
    public static readonly PermissionKey PayrollRun = new("payroll.run");
    public static readonly PermissionKey PayrollProcess = new("payroll.process");
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

    // Management permissions — these are implicitly granted to SuperAdmin and
    // Organization Admin roles within their scope.
    public static readonly PermissionKey UserManage = new("user.manage");
    public static readonly PermissionKey RoleManage = new("role.manage");
    public static readonly PermissionKey PermissionManage = new("permission.manage");
    public static readonly PermissionKey OrganizationManage = new("organization.manage");
    public static readonly PermissionKey WorkflowManage = new("workflow.manage");

    public static PermissionKey Create(string action) => new(action);
}

/// <summary>
/// Well-known role names used throughout the system.
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// The super admin role grants unrestricted access across all tenants and modules.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// The organization admin role can manage users and roles within their own organization.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// The default standard user role.
    /// </summary>
    public const string User = "User";

    public static bool IsSuperAdminRole(string? roleName) =>
        string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool IsAdminRole(string? roleName) =>
        string.Equals(roleName, Admin, StringComparison.Ordinal)
        || string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase);

    public static bool IsOrganizationAdminOrHigher(string? roleName) =>
        IsSuperAdminRole(roleName) || IsAdminRole(roleName);
}

/// <summary>
/// Centralised helper for resolving the current user's roles from HTTP claims.
/// </summary>
public static class RoleChecker
{
    public static bool IsSuperAdmin(string? userId, ClaimsPrincipal? user)
    {
        if (SuperAdmin.IsSuperAdmin(userId)) return true;
        if (user?.Identity?.IsAuthenticated != true) return false;
        return user.Claims.Any(c => c.Type == ClaimTypes.Role && RoleNames.IsSuperAdminRole(c.Value));
    }

    public static bool IsAdmin(string? userId, ClaimsPrincipal? user)
    {
        if (IsSuperAdmin(userId, user)) return true;
        if (user?.Identity?.IsAuthenticated != true) return false;
        return user.Claims.Any(c => c.Type == ClaimTypes.Role && RoleNames.IsAdminRole(c.Value));
    }

    public static bool HasRole(ClaimsPrincipal? user, string roleName)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;
        return user.Claims.Any(c => c.Type == ClaimTypes.Role && string.Equals(c.Value, roleName, StringComparison.OrdinalIgnoreCase));
    }

    public static string? GetUserId(ClaimsPrincipal? user) =>
        user?.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;

    public static string[] GetRoles(ClaimsPrincipal? user) =>
        user?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
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
    public Role(string id, string tenantId, string name, string? description = null)
    {
        Id = Tenant.Required(id, nameof(id), "Role ID is required.");
        TenantId = Tenant.Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        Name = Tenant.Required(name, nameof(name), "Role name is required.");
        Description = description;
    }
    public string Id { get; private set; } = null!;
    public string TenantId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public void Rename(string name)
    {
        Name = Tenant.Required(name, nameof(name), "Role name is required.");
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }
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
    Task<IReadOnlySet<string>> GetUserRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlySet<string>> GetUserRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        var membership = await db.UserMemberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (membership is null) return new HashSet<string>();

        var roleIds = await db.MembershipRoleAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.MembershipId == membership.Id)
            .Select(a => a.RoleId)
            .ToArrayAsync(cancellationToken);

        if (roleIds.Length == 0) return new HashSet<string>();

        var roleNames = await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToArrayAsync(cancellationToken);

        return new HashSet<string>(roleNames, StringComparer.OrdinalIgnoreCase);
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
        
        // Super admin bypasses all authorization checks
        if (SuperAdmin.IsSuperAdmin(request.Context.UserId))
        {
            return AuthorizationDecision.Allow();
        }
        
        var membership = await repository.GetMembershipForUserAsync(request.Context.TenantId, request.Context.UserId, cancellationToken);
        if (membership is null) return AuthorizationDecision.Deny(AuthorizationDenialReason.MissingMembership);
        if (!membership.IsActive) return AuthorizationDecision.Deny(AuthorizationDenialReason.InactiveMembership);

        var assignments = await repository.GetAssignmentsAsync(membership, cancellationToken);
        
        // Super admin role has all access and permissions
        if (assignments.Any(x => RoleNames.IsSuperAdminRole(x.Role.Name)))
        {
            return AuthorizationDecision.Allow();
        }
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
