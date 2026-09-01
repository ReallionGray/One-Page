using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Api.Endpoints;

public static class SuperAdminEndpoints
{
    public static void MapSuperAdminEndpoints(this WebApplication app)
    {
        // Super admin can create new tenants/organizations
        app.MapPost("/api/v1/super-admin/tenants", async (
            CreateTenantCommand command,
            ITenantContextAccessor ctx,
            ITenantRepository tenantRepo,
            IOrganizationRepository orgRepo,
            IAuditRepository audit,
            CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Only super admin can create tenants
            if (!SuperAdmin.IsSuperAdmin(current.UserId))
            {
                return Results.Problem(statusCode: 403, title: "Access denied", detail: "Only super admin can create tenants.");
            }
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(command.Id) || string.IsNullOrWhiteSpace(command.Name))
            {
                return Results.Problem(statusCode: 400, title: "Validation failed", detail: "Tenant ID and Name are required.");
            }
            
            // Check if tenant already exists
            var existing = await tenantRepo.GetAsync(command.Id, ct);
            if (existing is not null)
            {
                return Results.Problem(statusCode: 409, title: "Conflict", detail: "A tenant with this ID already exists.");
            }
            
            // Create the tenant
            var tenant = new Tenant(command.Id.Trim(), command.Name.Trim(), command.SubscriptionPlan);
            await tenantRepo.CreateAsync(tenant, ct);
            
            // Audit the tenant creation
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"),
                current.TenantId,
                current.UserId,
                $"tenant.create:{tenant.Id}",
                "tenant",
                tenant.Id,
                null,
                null,
                current.CorrelationId,
                null,
                null), ct);
            
            return Results.Created($"/api/v1/super-admin/tenants/{tenant.Id}", new 
            {
                tenant.Id,
                tenant.Name,
                tenant.SubscriptionPlan,
                tenant.CreatedAt
            });
        });
        
        // Super admin can list all tenants
        app.MapGet("/api/v1/super-admin/tenants", async (
            ITenantContextAccessor ctx,
            OrganizationDbContext db,
            CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            if (!SuperAdmin.IsSuperAdmin(current.UserId))
            {
                return Results.Problem(statusCode: 403, title: "Access denied", detail: "Only super admin can list all tenants.");
            }
            
            var tenants = await db.Tenants.ToListAsync(ct);
            return Results.Ok(tenants.Select(t => new 
            {
                t.Id,
                t.Name,
                t.SubscriptionPlan,
                t.CreatedAt
            }));
        });
        
        // Super admin can get a specific tenant
        app.MapGet("/api/v1/super-admin/tenants/{id}", async (
            string id,
            ITenantContextAccessor ctx,
            ITenantRepository tenantRepo,
            CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            if (!SuperAdmin.IsSuperAdmin(current.UserId))
            {
                return Results.Problem(statusCode: 403, title: "Access denied", detail: "Only super admin can view tenant details.");
            }
            
            var tenant = await tenantRepo.GetAsync(id, ct);
            if (tenant is null)
            {
                return Results.NotFound();
            }
            
            return Results.Ok(new 
            {
                tenant.Id,
                tenant.Name,
                tenant.SubscriptionPlan,
                tenant.CreatedAt
            });
        });
        
        // Super admin can update tenant subscription
        app.MapPost("/api/v1/super-admin/tenants/{id}/subscription", async (
            string id,
            UpdateTenantSubscriptionCommand command,
            ITenantContextAccessor ctx,
            ITenantRepository tenantRepo,
            IAuditRepository audit,
            CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            if (!SuperAdmin.IsSuperAdmin(current.UserId))
            {
                return Results.Problem(statusCode: 403, title: "Access denied", detail: "Only super admin can update tenant subscriptions.");
            }
            
            var tenant = await tenantRepo.GetAsync(id, ct);
            if (tenant is null)
            {
                return Results.NotFound();
            }
            
            tenant.UpgradeSubscription(command.SubscriptionPlan);
            await tenantRepo.CreateAsync(tenant, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"),
                current.TenantId,
                current.UserId,
                $"tenant.subscription.update:{id}",
                "tenant",
                id,
                null,
                null,
                current.CorrelationId,
                null,
                null), ct);
            
            return Results.Ok(new 
            {
                tenant.Id,
                tenant.Name,
                tenant.SubscriptionPlan
            });
        });
        
        // Super admin can create users/memberships in any tenant
        app.MapPost("/api/v1/super-admin/tenants/{tenantId}/users", async (
            string tenantId,
            CreateUserCommand command,
            ITenantContextAccessor ctx,
            OrganizationDbContext db,
            IAuditRepository audit,
            CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            if (!SuperAdmin.IsSuperAdmin(current.UserId))
            {
                return Results.Problem(statusCode: 403, title: "Access denied", detail: "Only super admin can create users.");
            }
            
            // Verify tenant exists
            var tenant = await db.Tenants.FindAsync(new object[] { tenantId }, ct);
            if (tenant is null)
            {
                return Results.NotFound();
            }
            
            // Validate userId
            if (string.IsNullOrWhiteSpace(command.UserId))
            {
                return Results.Problem(statusCode: 400, title: "Validation failed", detail: "User ID is required.");
            }
            
            // Create membership
            var membership = new UserMembership(Guid.NewGuid().ToString("N"), tenantId, command.UserId);
            db.UserMemberships.Add(membership);
            
            // Create admin role if it doesn't exist
            var roleId = $"role-{RoleNames.Admin}-{tenantId}";
            if (!await db.Roles.AnyAsync<Role>(r => r.TenantId == tenantId && r.Name == RoleNames.Admin, ct))
            {
                db.Roles.Add(new Role(roleId, tenantId, RoleNames.Admin));
            }
            
            // Assign all permissions to admin role (including management permissions)
            var allPermissions = new[] {
                PermissionCatalog.AssetCreate, PermissionCatalog.AssetView, PermissionCatalog.AssetAssign, PermissionCatalog.AssetDispose,
                PermissionCatalog.AssetTransfer,
                PermissionCatalog.PurchaseOrderCreate, PermissionCatalog.PurchaseOrderApprove,
                PermissionCatalog.InventoryAdjust, PermissionCatalog.InventoryView,
                PermissionCatalog.PosSaleCreate,
                PermissionCatalog.FinanceJournalExport,
                PermissionCatalog.ApprovalReview,
                PermissionCatalog.ReportRun, PermissionCatalog.ReportExport,
                PermissionCatalog.EmployeeView, PermissionCatalog.PayrollRun,
                PermissionCatalog.UserManage, PermissionCatalog.RoleManage, PermissionCatalog.PermissionManage
            };
            
            foreach (var perm in allPermissions)
            {
                var permId = $"rp-{roleId}-{perm.ToString().Replace(".", "-")}";
                if (!await db.RolePermissions.AnyAsync<RolePermission>(rp => rp.TenantId == tenantId && rp.RoleId == roleId && rp.Permission == perm.ToString(), ct))
                {
                    db.RolePermissions.Add(new RolePermission(permId, tenantId, roleId, perm));
                }
            }
            
            // Assign membership to admin role
            var membershipAssignmentId = Guid.NewGuid().ToString("N");
            if (!await db.MembershipRoleAssignments.AnyAsync<MembershipRoleAssignment>(a => a.TenantId == tenantId && a.MembershipId == membership.Id && a.RoleId == roleId, ct))
            {
                db.MembershipRoleAssignments.Add(new MembershipRoleAssignment(membershipAssignmentId, tenantId, membership.Id, roleId));
            }
            
            // Grant entitlements for the tenant
            var modules = new[] {
                EntitlementKeys.Modules.Hr, EntitlementKeys.Modules.Payroll, EntitlementKeys.Modules.Assets,
                EntitlementKeys.Modules.Procurement, EntitlementKeys.Modules.Inventory, EntitlementKeys.Modules.Pos,
                EntitlementKeys.Modules.Finance, EntitlementKeys.Modules.Reporting
            };
            
            foreach (var module in modules)
            {
                var entitlementAssignmentId = $"ent-{tenantId}-{module.Namespace}-{module.Name}";
                if (!await db.EntitlementAssignments.AnyAsync<EntitlementAssignment>(e => e.TenantId == tenantId && e.EntitlementNamespace == module.Namespace && e.EntitlementName == module.Name, ct))
                {
                    db.EntitlementAssignments.Add(new EntitlementAssignment(
                        entitlementAssignmentId, tenantId, module.Namespace, module.Name, 
                        EntitlementState.Active, command.SubscriptionPlan.ToString()));
                }
            }
            
            await db.SaveChangesAsync(ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"),
                current.TenantId,
                current.UserId,
                $"user.create:{command.UserId}",
                "user",
                command.UserId,
                null,
                null,
                current.CorrelationId,
                null,
                null), ct);
            
            return Results.Created($"/api/v1/super-admin/tenants/{tenantId}/users/{command.UserId}", new 
            {
                membership.Id,
                membership.UserId,
                membership.TenantId,
                membership.IsActive,
                Role = "admin",
                Permissions = allPermissions.Select(p => p.ToString()).ToList()
            });
        });
        
        // Super admin can list users in a tenant
        app.MapGet("/api/v1/super-admin/tenants/{tenantId}/users", async (
            string tenantId,
            ITenantContextAccessor ctx,
            OrganizationDbContext db,
            CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            if (!SuperAdmin.IsSuperAdmin(current.UserId))
            {
                return Results.Problem(statusCode: 403, title: "Access denied", detail: "Only super admin can list users.");
            }
            
            var memberships = await db.UserMemberships
                .Where(m => m.TenantId == tenantId)
                .ToListAsync(ct);
            
            return Results.Ok(memberships.Select(m => new 
            {
                m.Id,
                m.UserId,
                m.TenantId,
                m.IsActive,
                m.CreatedAt
            }));
        });
    }
}

public sealed class CreateTenantCommand
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Professional;
}

public sealed class UpdateTenantSubscriptionCommand
{
    public SubscriptionPlan SubscriptionPlan { get; set; }
}

public sealed class CreateUserCommand
{
    public string? UserId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Professional;
}
