using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Platform.Tests;

public sealed class AuthorizationTests : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private OrganizationDbContext db = null!;
    private IAuthorizationEvaluator evaluator = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        db = new OrganizationDbContext(new DbContextOptionsBuilder<OrganizationDbContext>().UseSqlite(connection).Options);
        await OrganizationPersistence.InitializeAsync(db);
        evaluator = new ScopedAuthorizationEvaluator(new AuthorizationRepository(db));
        await db.Tenants.AddAsync(new Tenant("tenant-1", "Acme"));
        await db.UserMemberships.AddAsync(new UserMembership("membership-1", "tenant-1", "user-1"));
        await db.Roles.AddAsync(new Role("role-1", "tenant-1", "Finance"));
        await db.RolePermissions.AddAsync(new RolePermission("permission-1", "tenant-1", "role-1", PermissionCatalog.PurchaseOrderApprove));
        await db.MembershipRoleAssignments.AddAsync(new MembershipRoleAssignment("assignment-1", "tenant-1", "membership-1", "role-1", branchId: "branch-1", amountLimit: 5000, currency: "NGN"));
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }

    private static TenantContext Context(string tenant = "tenant-1", string user = "user-1") => TenantContext.Create(user, tenant, "corr-1");

    [Fact]
    public async Task Authorized_action_requires_active_membership_and_permission()
    {
        var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(), PermissionCatalog.PurchaseOrderApprove, new AuthorizationScope(BranchId: "branch-1"), 100));
        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Missing_membership_and_cross_tenant_are_distinct_denials()
    {
        var missing = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(user: "unknown"), PermissionCatalog.PurchaseOrderApprove));
        var crossTenant = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(tenant: "tenant-2"), PermissionCatalog.PurchaseOrderApprove));
        Assert.Equal(AuthorizationDenialReason.MissingMembership, missing.DenialReason);
        Assert.Equal(AuthorizationDenialReason.MissingMembership, crossTenant.DenialReason);
    }

    [Fact]
    public async Task Inactive_membership_is_denied_before_permission_evaluation()
    {
        var membership = await db.UserMemberships.SingleAsync(x => x.Id == "membership-1");
        membership.SetActive(false);
        await db.SaveChangesAsync();
        var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(), PermissionCatalog.PurchaseOrderApprove));
        Assert.Equal(AuthorizationDenialReason.InactiveMembership, decision.DenialReason);
    }

    [Fact]
    public async Task Permission_scope_and_amount_limit_have_typed_denials()
    {
        var missingPermission = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(), PermissionCatalog.PayrollRun, new AuthorizationScope(BranchId: "branch-1")));
        var missingScope = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(), PermissionCatalog.PurchaseOrderApprove, new AuthorizationScope(BranchId: "branch-2"), 100));
        var exceeded = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(), PermissionCatalog.PurchaseOrderApprove, new AuthorizationScope(BranchId: "branch-1"), 5000.01m));
        Assert.Equal(AuthorizationDenialReason.MissingPermission, missingPermission.DenialReason);
        Assert.Equal(AuthorizationDenialReason.ScopeMismatch, missingScope.DenialReason);
        Assert.Equal(AuthorizationDenialReason.AmountLimitExceeded, exceeded.DenialReason);
    }

    [Fact]
    public async Task Manager_chain_scope_matches_an_ancestor()
    {
        var role = new Role("role-2", "tenant-1", "HR");
        await db.Roles.AddAsync(role);
        await db.RolePermissions.AddAsync(new RolePermission("permission-2", "tenant-1", "role-2", PermissionCatalog.EmployeeView));
        await db.MembershipRoleAssignments.AddAsync(new MembershipRoleAssignment("assignment-2", "tenant-1", "membership-1", "role-2", managerUserId: "manager-1"));
        await db.SaveChangesAsync();
        var decision = await evaluator.AuthorizeAsync(new AuthorizationRequest(Context(), PermissionCatalog.EmployeeView, new AuthorizationScope(ManagerChainUserIds: new HashSet<string> { "manager-1" })));
        Assert.True(decision.Allowed);
    }
}
