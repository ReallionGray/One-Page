using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnePage.Api;
using OnePage.Platform;

namespace OnePage.Platform.Tests;

/// <summary>
/// Tests for RoleChecker, RoleNames, PermissionCatalog management permissions,
/// and the SuperAdmin all-access bypass in the authorization evaluator.
/// </summary>
public sealed class RoleManagementTests : IAsyncLifetime
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

        // Seed demo data to test role and permission seeding
        await db.Tenants.AddAsync(new Tenant("demo-tenant", "Demo Tenant", SubscriptionPlan.Enterprise));
        await db.Tenants.AddAsync(new Tenant("acme-tenant", "Acme Corp", SubscriptionPlan.Professional));
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }

    // ---- RoleChecker tests ----

    [Fact]
    public void RoleChecker_IsSuperAdmin_returns_true_for_superadmin_user_id()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "super-admin"),
            new Claim(ClaimTypes.NameIdentifier, "super-admin"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.True(RoleChecker.IsSuperAdmin("super-admin", user));
    }

    [Fact]
    public void RoleChecker_IsSuperAdmin_returns_true_for_superadmin_role_claim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-123"),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.True(RoleChecker.IsSuperAdmin("user-123", user));
    }

    [Fact]
    public void RoleChecker_IsSuperAdmin_returns_false_for_regular_user()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-123"),
            new Claim(ClaimTypes.Role, "user"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.False(RoleChecker.IsSuperAdmin("user-123", user));
    }

    [Fact]
    public void RoleChecker_IsAdmin_returns_true_for_admin_role_claim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-123"),
            new Claim(ClaimTypes.Role, "Admin"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.True(RoleChecker.IsAdmin("user-123", user));
    }

    [Fact]
    public void RoleChecker_IsAdmin_is_case_insensitive()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-123"),
            new Claim(ClaimTypes.Role, "admin"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.True(RoleChecker.IsAdmin("user-123", user));
    }

    [Fact]
    public void RoleChecker_IsAdmin_returns_false_for_non_admin()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-123"),
            new Claim(ClaimTypes.Role, "user"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.False(RoleChecker.IsAdmin("user-123", user));
    }

    [Fact]
    public void RoleChecker_GetUserId_returns_user_id_claim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-abc"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.Equal("user-abc", RoleChecker.GetUserId(user));
    }

    [Fact]
    public void RoleChecker_HasRole_returns_true_for_matching_role()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("user_id", "user-123"),
            new Claim(ClaimTypes.Role, "Admin"),
        }, "test");

        var user = new ClaimsPrincipal(identity);
        Assert.True(RoleChecker.HasRole(user, "Admin"));
        Assert.True(RoleChecker.HasRole(user, "admin")); // case insensitive
        Assert.False(RoleChecker.HasRole(user, "SuperAdmin"));
    }

    // ---- RoleNames tests ----

    [Fact]
    public void RoleNames_constants_have_expected_values()
    {
        Assert.Equal("SuperAdmin", RoleNames.SuperAdmin);
        Assert.Equal("Admin", RoleNames.Admin);
        Assert.Equal("User", RoleNames.User);
    }

    [Fact]
    public void RoleNames_IsSuperAdminRole_returns_true_for_superadmin()
    {
        Assert.True(RoleNames.IsSuperAdminRole("SuperAdmin"));
        Assert.True(RoleNames.IsSuperAdminRole(new Role("role-1", "tenant-1", "SuperAdmin").Name));
        Assert.False(RoleNames.IsSuperAdminRole("Admin"));
    }

    [Fact]
    public void RoleNames_IsAdminRole_returns_true_for_admin()
    {
        Assert.True(RoleNames.IsAdminRole("Admin"));
        Assert.True(RoleNames.IsAdminRole("admin")); // case insensitive
        Assert.False(RoleNames.IsAdminRole("User"));
    }

    [Fact]
    public void RoleNames_IsOrganizationAdminOrHigher_returns_true_for_admin_or_superadmin()
    {
        Assert.True(RoleNames.IsOrganizationAdminOrHigher("Admin"));
        Assert.True(RoleNames.IsOrganizationAdminOrHigher("SuperAdmin"));
        Assert.False(RoleNames.IsOrganizationAdminOrHigher("user"));
    }

    // ---- PermissionCatalog management permissions ----

    [Fact]
    public void PermissionCatalog_has_management_permissions()
    {
        Assert.Equal("user.manage", PermissionCatalog.UserManage.ToString());
        Assert.Equal("role.manage", PermissionCatalog.RoleManage.ToString());
        Assert.Equal("permission.manage", PermissionCatalog.PermissionManage.ToString());
        Assert.Equal("organization.manage", PermissionCatalog.OrganizationManage.ToString());
    }

    // ---- SuperAdmin role has all-access bypass in evaluator ----

    [Fact]
    public async Task ScopedAuthorizationEvaluator_grants_all_permissions_for_superadmin()
    {
        // Set up a SuperAdmin member
        await db.UserMemberships.AddAsync(new UserMembership("mem-superadmin", "demo-tenant", "super-admin"));
        var superAdminRole = new Role("role-superadmin", "demo-tenant", "SuperAdmin");
        await db.Roles.AddAsync(superAdminRole);
        await db.MembershipRoleAssignments.AddAsync(
            new MembershipRoleAssignment("assign-superadmin", "demo-tenant", "mem-superadmin", superAdminRole.Id));
        await db.SaveChangesAsync();

        var context = TenantContext.Create("super-admin", "demo-tenant", "corr-1");

        // Test a regular permission not explicitly granted to the role
        var decision = await evaluator.AuthorizeAsync(
            new AuthorizationRequest(context, PermissionCatalog.PurchaseOrderApprove));

        // SuperAdmin should be granted all permissions even if the role doesn't have them explicitly
        Assert.True(decision.Allowed);
    }

    // ---- DemoData seeds management permissions ----

    [Fact]
    public async Task DemoData_seeds_management_permissions_for_superadmin_and_admin_roles()
    {
        var (provider, demoDb, demoConnection) = CreateDemoServices();

        await DemoData.SeedAsync(provider, CancellationToken.None);

        // SuperAdmin role should have management permissions
        var superAdminRolePerms = await demoDb.RolePermissions
            .Where(rp => rp.TenantId == "demo-tenant" &&
                         rp.RoleId == "role-SuperAdmin")
            .Select(rp => rp.Permission)
            .ToListAsync();

        Assert.Contains(PermissionCatalog.UserManage.ToString(), superAdminRolePerms);
        Assert.Contains(PermissionCatalog.RoleManage.ToString(), superAdminRolePerms);
        Assert.Contains(PermissionCatalog.PermissionManage.ToString(), superAdminRolePerms);
        Assert.Contains(PermissionCatalog.OrganizationManage.ToString(), superAdminRolePerms);

        // Admin role should have management permissions
        var adminRolePerms = await demoDb.RolePermissions
            .Where(rp => rp.TenantId == "demo-tenant" && rp.RoleId == "role-admin")
            .Select(rp => rp.Permission)
            .ToListAsync();

        Assert.Contains(PermissionCatalog.UserManage.ToString(), adminRolePerms);
        Assert.Contains(PermissionCatalog.RoleManage.ToString(), adminRolePerms);
        Assert.Contains(PermissionCatalog.PermissionManage.ToString(), adminRolePerms);

        await CleanupDemoServices(demoDb, demoConnection);
    }

    [Fact]
    public async Task DemoData_seeds_management_permissions_for_acme_tenant_admin()
    {
        var (provider, demoDb, demoConnection) = CreateDemoServices();

        await DemoData.SeedAsync(provider, CancellationToken.None);

        // Acme tenant admin role should have management permissions
        var acmeAdminRolePerms = await demoDb.RolePermissions
            .Where(rp => rp.TenantId == "acme-tenant" && rp.RoleId == "acme-role-admin")
            .Select(rp => rp.Permission)
            .ToListAsync();

        Assert.Contains(PermissionCatalog.UserManage.ToString(), acmeAdminRolePerms);
        Assert.Contains(PermissionCatalog.RoleManage.ToString(), acmeAdminRolePerms);
        Assert.Contains(PermissionCatalog.PermissionManage.ToString(), acmeAdminRolePerms);

        await CleanupDemoServices(demoDb, demoConnection);
    }

    private static (IServiceProvider provider, OrganizationDbContext db, SqliteConnection connection) CreateDemoServices()
    {
        var demoConnection = new SqliteConnection("Data Source=:memory:");
        demoConnection.Open();
        var services = new ServiceCollection();
        services.AddSingleton<InMemoryEntitlementEvaluator>();
        services.AddSingleton<IEntitlementEvaluator>(sp => sp.GetRequiredService<InMemoryEntitlementEvaluator>());
        services.AddDbContext<OrganizationDbContext>(options => options.UseSqlite(demoConnection));
        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<OrganizationDbContext>();
        OrganizationPersistence.InitializeAsync(db).Wait();
        return (provider, db, demoConnection);
    }

    private static async Task CleanupDemoServices(OrganizationDbContext db, SqliteConnection connection)
    {
        await db.DisposeAsync();
        await connection.DisposeAsync();
    }
}
