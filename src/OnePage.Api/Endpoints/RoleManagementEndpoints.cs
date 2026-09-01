using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;
using System.Security.Claims;

namespace OnePage.Api.Endpoints;

/// <summary>
/// API endpoints for managing roles and permissions.
/// SuperAdmin can manage roles across all organizations; Organization Admins
/// can manage roles only within their own organization.
/// </summary>
public static class RoleManagementEndpoints
{
    public static void MapRoleManagementEndpoints(this WebApplication app)
    {
        // GET /api/v1/roles — list all roles
        // SuperAdmin: all organizations | OrgAdmin: own organization only
        app.MapGet("/api/v1/roles", async (HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(userId, context.User);
            var isAdmin = RoleChecker.IsAdmin(userId, context.User);

            IQueryable<Role> query;

            if (isSuperAdmin)
            {
                // Super admin can see all roles across all organizations
                query = db.Roles.AsNoTracking();
            }
            else if (isAdmin)
            {
                // Org admin can only see roles in their organization
                var tenantId = current.TenantId;
                query = db.Roles.AsNoTracking().Where(r => r.TenantId == tenantId);
            }
            else
            {
                return Results.Forbid();
            }

            var dbRoles = await query
                .OrderBy(r => r.TenantId)
                .ThenBy(r => r.Name)
                .ToListAsync(ct);

            var dbRoleIds = dbRoles.Select(r => r.Id).ToList();
            var dbPermissions = await db.RolePermissions
                .AsNoTracking()
                .Where(rp => dbRoleIds.Contains(rp.RoleId))
                .ToListAsync(ct);

            var permissionsByRole = dbPermissions
                .GroupBy(rp => rp.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(rp => rp.Permission).ToList());

            var roles = dbRoles.Select(r => new
            {
                r.Id,
                r.TenantId,
                r.Name,
                r.Description,
                Permissions = permissionsByRole.TryGetValue(r.Id, out var p) ? p : new List<string>()
            }).ToList();

            return Results.Ok(new { roles });
        });

        // GET /api/v1/roles/{roleId} — get a specific role with details
        // SuperAdmin: any organization | OrgAdmin: own organization only
        app.MapGet("/api/v1/roles/{roleId}", async (string roleId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(userId, context.User);
            var isAdmin = RoleChecker.IsAdmin(userId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var role = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound();

            // If not super admin, check if role belongs to their organization
            if (!isSuperAdmin && role.TenantId != current.TenantId)
                return Results.Forbid();

            var rolePermissions = await db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync(ct);

            var assignments = await db.MembershipRoleAssignments
                .AsNoTracking()
                .Where(mra => mra.RoleId == roleId)
                .ToListAsync(ct);

            var assignmentMembershipIds = assignments.Select(mra => mra.MembershipId).ToList();
            var memberships = await db.UserMemberships
                .AsNoTracking()
                .Where(m => assignmentMembershipIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, ct);

            var usersWithRole = assignments
                .Where(mra => memberships.ContainsKey(mra.MembershipId))
                .Select(mra => new
                {
                    userId = memberships[mra.MembershipId].UserId,
                    tenantId = mra.TenantId
                })
                .ToList();

            return Results.Ok(new
            {
                role.Id,
                role.TenantId,
                role.Name,
                role.Description,
                Permissions = rolePermissions,
                UserCount = usersWithRole.Count,
                Users = usersWithRole
            });
        });

        // POST /api/v1/roles — create a new role with permissions
        // SuperAdmin: any organization (specify TenantId) | OrgAdmin: own organization only
        app.MapPost("/api/v1/roles", async (CreateRoleRequest request, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(userId, context.User);
            var isAdmin = RoleChecker.IsAdmin(userId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var tenantId = isSuperAdmin ? request.TenantId : current.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
                return Results.BadRequest(new { message = "TenantId is required for role creation" });

            // Check if role with same name already exists in the organization
            var existingRole = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == request.Name, ct);

            if (existingRole != null)
                return Results.Conflict(new { message = "Role with this name already exists in the organization" });

            var roleId = Guid.NewGuid().ToString();
            var role = new Role(roleId, tenantId, request.Name, request.Description);
            db.Roles.Add(role);

            // Add permissions
            if (request.Permissions is not null)
            {
                foreach (var permission in request.Permissions)
                {
                    var permissionId = Guid.NewGuid().ToString();
                    var rolePermission = new RolePermission(permissionId, tenantId, roleId, permission);
                    db.RolePermissions.Add(rolePermission);
                }
            }

            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/roles/{roleId}", new
            {
                roleId,
                message = "Role created successfully"
            });
        });

        // PUT /api/v1/roles/{roleId} — update a role's name, description, and permissions
        // SuperAdmin: any organization | OrgAdmin: own organization only
        app.MapPut("/api/v1/roles/{roleId}", async (string roleId, UpdateRoleRequest request, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(userId, context.User);
            var isAdmin = RoleChecker.IsAdmin(userId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var role = await db.Roles.FindAsync([roleId], ct);
            if (role == null)
                return Results.NotFound();

            // If not super admin, check if role belongs to their organization
            if (!isSuperAdmin && role.TenantId != current.TenantId)
                return Results.Forbid();

            // Update role properties
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != role.Name)
            {
                // Check if new name is already taken
                var nameExists = await db.Roles.AsNoTracking()
                    .AnyAsync(r => r.TenantId == role.TenantId && r.Name == request.Name && r.Id != roleId, ct);

                if (nameExists)
                    return Results.Conflict(new { message = "Role with this name already exists" });

                role.Rename(request.Name);
            }

            if (request.Description != null)
                role.SetDescription(request.Description);

            // Update permissions
            if (request.Permissions != null)
            {
                var existingPermissions = await db.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync(ct);

                db.RolePermissions.RemoveRange(existingPermissions);

                foreach (var permission in request.Permissions)
                {
                    var permissionId = Guid.NewGuid().ToString();
                    var rolePermission = new RolePermission(permissionId, role.TenantId, roleId, permission);
                    db.RolePermissions.Add(rolePermission);
                }
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Role updated successfully" });
        });

        // DELETE /api/v1/roles/{roleId} — delete a role
        // SuperAdmin: all organizations | OrgAdmin: own organization only
        app.MapDelete("/api/v1/roles/{roleId}", async (string roleId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(userId, context.User);
            var isAdmin = RoleChecker.IsAdmin(userId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var role = await db.Roles.FindAsync([roleId], ct);
            if (role == null)
                return Results.NotFound();

            // If not super admin, check if role belongs to their organization
            if (!isSuperAdmin && role.TenantId != current.TenantId)
                return Results.Forbid();

            // Prevent deletion of protected system roles
            if (RoleNames.IsOrganizationAdminOrHigher(role.Name))
                return Results.BadRequest(new { message = $"Cannot delete protected system role: {role.Name}" });

            // Check if role is assigned to any users
            var hasAssignments = await db.MembershipRoleAssignments
                .AnyAsync(mra => mra.RoleId == roleId, ct);

            if (hasAssignments)
                return Results.BadRequest(new { message = "Cannot delete role that is assigned to users" });

            // Delete permissions
            var permissions = await db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync(ct);

            db.RolePermissions.RemoveRange(permissions);
            db.Roles.Remove(role);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Role deleted successfully" });
        });

        // POST /api/v1/roles/{roleId}/users/{userId} — assign a role to a user
        // SuperAdmin: any organization | OrgAdmin: own organization only
        app.MapPost("/api/v1/roles/{roleId}/users/{userId}", async (string roleId, string userId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var role = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound();

            // If not super admin, check if role belongs to their organization
            if (!isSuperAdmin && role.TenantId != current.TenantId)
                return Results.Forbid();

            // Find user's membership in the role's organization
            var membership = await db.UserMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == role.TenantId && m.IsActive, ct);

            if (membership == null)
                return Results.BadRequest(new { message = "User does not have an active membership in this organization" });

            // Check if role is already assigned
            var existingAssignment = await db.MembershipRoleAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(mra => mra.MembershipId == membership.Id && mra.RoleId == roleId, ct);

            if (existingAssignment != null)
                return Results.Conflict(new { message = "Role already assigned to user in this organization" });

            var assignmentId = Guid.NewGuid().ToString();
            var assignment = new MembershipRoleAssignment(assignmentId, role.TenantId, membership.Id, roleId);
            db.MembershipRoleAssignments.Add(assignment);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Role assigned to user successfully" });
        });

        // DELETE /api/v1/roles/{roleId}/users/{userId} — remove a role from a user
        // SuperAdmin: any organization | OrgAdmin: own organization only
        app.MapDelete("/api/v1/roles/{roleId}/users/{userId}", async (string roleId, string userId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var role = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound();

            // If not super admin, check if role belongs to their organization
            if (!isSuperAdmin && role.TenantId != current.TenantId)
                return Results.Forbid();

            // Prevent removing protected system roles from users
            if (RoleNames.IsOrganizationAdminOrHigher(role.Name))
                return Results.BadRequest(new { message = $"Cannot remove protected system role: {role.Name}" });

            var membership = await db.UserMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == role.TenantId && m.IsActive, ct);

            if (membership == null)
                return Results.NotFound();

            var assignment = await db.MembershipRoleAssignments
                .FirstOrDefaultAsync(mra => mra.MembershipId == membership.Id && mra.RoleId == roleId, ct);

            if (assignment == null)
                return Results.NotFound();

            db.MembershipRoleAssignments.Remove(assignment);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Role removed from user successfully" });
        });
    }
}

public record CreateRoleRequest(
    string Name,
    string? Description = null,
    string[]? Permissions = null,
    string? TenantId = null // Required for SuperAdmin to specify organization
);

public record UpdateRoleRequest(
    string? Name = null,
    string? Description = null,
    string[]? Permissions = null
);
