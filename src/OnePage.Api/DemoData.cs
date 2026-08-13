using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Api;

internal static class DemoData
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var ent = scope.ServiceProvider.GetRequiredService<InMemoryEntitlementEvaluator>();

        // Seed demo tenant and membership
        const string tenantId = "demo-tenant";
        const string userId = "demo-user";
        const string membershipId = "membership-demo-user";
        var existing = await db.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
        if (existing is null)
        {
            db.Tenants.Add(new Tenant(tenantId, "Demo Tenant"));
        }

        var membership = await db.UserMemberships.SingleOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (membership is null)
        {
            db.UserMemberships.Add(new UserMembership(membershipId, tenantId, userId));
        }

        // Add an admin role with broad permissions
        var roleId = "role-admin";
        var role = await db.Roles.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Name == "admin", cancellationToken);
        if (role is null) db.Roles.Add(new Role(roleId, tenantId, "admin"));

        // Ensure permissions exist for the role (duplicates ignored by unique constraint at save)
        var perms = new[] {
            PermissionCatalog.AssetCreate, PermissionCatalog.AssetView, PermissionCatalog.AssetAssign, PermissionCatalog.AssetDispose, PermissionCatalog.ApprovalReview,
            PermissionCatalog.PurchaseOrderCreate, PermissionCatalog.PurchaseOrderApprove, PermissionCatalog.InventoryAdjust, PermissionCatalog.PosSaleCreate,
            PermissionCatalog.FinanceJournalExport, PermissionCatalog.ReportRun
        };
        foreach (var p in perms)
        {
            var rpId = $"rp-{roleId}-{p.ToString().Replace('.', '-') }";
            if (!await db.RolePermissions.AnyAsync(rp => rp.TenantId == tenantId && rp.RoleId == roleId && rp.Permission == p.ToString(), cancellationToken))
            {
                db.RolePermissions.Add(new RolePermission(rpId, tenantId, roleId, p));
            }
        }

        // Link membership to role
        if (!await db.MembershipRoleAssignments.AnyAsync(a => a.TenantId == tenantId && a.MembershipId == membershipId && a.RoleId == roleId, cancellationToken))
        {
            db.MembershipRoleAssignments.Add(new MembershipRoleAssignment(Guid.NewGuid().ToString("N"), tenantId, membershipId, roleId));
        }

        await db.SaveChangesAsync(cancellationToken);

        // Set entitlements active for demo tenant
        var defActive = new EntitlementDefinition(EntitlementKeys.Modules.Assets, EntitlementState.Active, "demo-seed");
        ent.Set(tenantId, defActive);
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Procurement, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Inventory, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Pos, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Finance, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Reporting, EntitlementState.Active, "demo-seed"));
    }
}
