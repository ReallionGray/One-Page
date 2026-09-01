using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;
using System.Security.Claims;

namespace OnePage.Api.Endpoints;

/// <summary>
/// API endpoints for managing users across organizations.
/// SuperAdmin can manage users in all organizations; Organization Admins
/// can manage users only within their own organization.
/// </summary>
public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this WebApplication app)
    {
        // GET /api/v1/users — list all users
        // SuperAdmin: all organizations | OrgAdmin: own organization only
        app.MapGet("/api/v1/users", async (HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var userId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(userId, context.User);
            var isAdmin = RoleChecker.IsAdmin(userId, context.User);

            IQueryable<UserProfile> query;

            if (isSuperAdmin)
            {
                // Super admin can see all users across all organizations
                query = db.UserProfiles.AsNoTracking();
            }
            else if (isAdmin)
            {
                // Org admin can only see users in their organization
                var tenantId = current.TenantId;
                var memberUserIds = await db.UserMemberships
                    .AsNoTracking()
                    .Where(m => m.TenantId == tenantId && m.IsActive)
                    .Select(m => m.UserId)
                    .ToListAsync(ct);

                query = db.UserProfiles.AsNoTracking().Where(p => memberUserIds.Contains(p.UserId));
            }
            else
            {
                return Results.Forbid();
            }

            var users = await query
                .Select(p => new
                {
                    p.UserId,
                    p.FirstName,
                    p.LastName,
                    p.Email,
                    p.ProfileImageUrl,
                    p.JobTitle,
                    p.PhoneNumber,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync(ct);

            // Add organization and role info for each user
            var result = new List<object>();
            foreach (var userProfile in users)
            {
                var memberships = await db.UserMemberships
                    .AsNoTracking()
                    .Where(m => m.UserId == userProfile.UserId && m.IsActive)
                    .ToListAsync(ct);

                var tenantIds = memberships.Select(m => m.TenantId).ToList();
                var tenants = await db.Tenants
                    .AsNoTracking()
                    .Where(t => tenantIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id, ct);

                var organizations = memberships.Select(m => new
                {
                    tenantId = m.TenantId,
                    tenantName = tenants.TryGetValue(m.TenantId, out var t) ? t.Name : m.TenantId,
                    isActive = m.IsActive
                }).ToList();

                var roleAssignments = await db.MembershipRoleAssignments
                    .AsNoTracking()
                    .Where(mra => memberships.Select(m => m.Id).Contains(mra.MembershipId))
                    .ToListAsync(ct);

                var roleIds = roleAssignments.Select(mra => mra.RoleId).ToList();
                var dbRoles = await db.Roles
                    .AsNoTracking()
                    .Where(r => roleIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, ct);

                var roles = roleAssignments.Select(mra => new
                {
                    tenantId = mra.TenantId,
                    roleName = dbRoles.TryGetValue(mra.RoleId, out var r) ? r.Name : mra.RoleId
                }).ToList();

                result.Add(new
                {
                    userProfile.UserId,
                    userProfile.FirstName,
                    userProfile.LastName,
                    userProfile.Email,
                    userProfile.ProfileImageUrl,
                    userProfile.JobTitle,
                    userProfile.PhoneNumber,
                    userProfile.CreatedAt,
                    userProfile.UpdatedAt,
                    organizations,
                    roles
                });
            }

            return Results.Ok(new { users = result });
        });

        // GET /api/v1/users/{userId} — get a specific user
        // SuperAdmin/Admin: any user in same organization | User: own profile only
        app.MapGet("/api/v1/users/{userId}", async (string userId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            // Users can view their own profile
            var isOwnProfile = string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase);

            if (!isSuperAdmin && !isAdmin && !isOwnProfile)
                return Results.Forbid();

            var userProfile = await db.UserProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (userProfile == null)
                return Results.NotFound();

            // If not super admin and not own profile, check if user is in same organization
            if (!isSuperAdmin && !isOwnProfile)
            {
                var tenantId = current.TenantId;
                var userInOrg = await db.UserMemberships
                    .AsNoTracking()
                    .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive, ct);

                if (!userInOrg)
                    return Results.Forbid();
            }

            var memberships = await db.UserMemberships
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.IsActive)
                .ToListAsync(ct);

            var tenantIds = memberships.Select(m => m.TenantId).ToList();
            var tenants = await db.Tenants
                .AsNoTracking()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, ct);

            var organizations = memberships.Select(m => new
            {
                tenantId = m.TenantId,
                tenantName = tenants.TryGetValue(m.TenantId, out var t) ? t.Name : m.TenantId,
                isActive = m.IsActive
            }).ToList();

            var roleAssignments = await db.MembershipRoleAssignments
                .AsNoTracking()
                .Where(mra => memberships.Select(m => m.Id).Contains(mra.MembershipId))
                .ToListAsync(ct);

            var roleIds = roleAssignments.Select(mra => mra.RoleId).ToList();
            var dbRoles = await db.Roles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, ct);

            var roles = roleAssignments.Select(mra => new
            {
                tenantId = mra.TenantId,
                roleName = dbRoles.TryGetValue(mra.RoleId, out var r) ? r.Name : mra.RoleId
            }).ToList();

            return Results.Ok(new
            {
                userProfile.UserId,
                userProfile.FirstName,
                userProfile.LastName,
                userProfile.Email,
                userProfile.ProfileImageUrl,
                userProfile.JobTitle,
                userProfile.PhoneNumber,
                userProfile.Bio,
                userProfile.TimeZone,
                userProfile.PreferredLanguage,
                userProfile.CreatedAt,
                userProfile.UpdatedAt,
                organizations,
                roles
            });
        });

        // POST /api/v1/users — create a new user
        // SuperAdmin: any organization | OrgAdmin: own organization only
        app.MapPost("/api/v1/users", async (CreateUserRequest request, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, IPasswordHasher passwordHasher, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");

            // Org admin can only create users in their own organization
            if (!isSuperAdmin)
            {
                if (request.TenantIds == null || request.TenantIds.Length != 1 || request.TenantIds[0] != current.TenantId)
                {
                    return Results.BadRequest(new { message = "Admins can only create users in their own organization" });
                }
            }

            // Check if email already exists
            var existingProfile = await db.UserProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == request.Email, ct);

            if (existingProfile != null)
                return Results.Conflict(new { message = "User with this email already exists" });

            var newUserId = Guid.NewGuid().ToString();
            var hashedPassword = passwordHasher.HashPassword(request.Password);

            var userProfile = new UserProfile(newUserId, request.FirstName, request.LastName, request.Email);
            userProfile.UpdatePassword(hashedPassword);
            if (!string.IsNullOrWhiteSpace(request.ProfileImageUrl))
                userProfile.UpdateProfileImage(request.ProfileImageUrl);

            db.UserProfiles.Add(userProfile);

            // Create memberships for specified organizations
            foreach (var tenantId in request.TenantIds)
            {
                var membershipId = Guid.NewGuid().ToString();
                var membership = new UserMembership(membershipId, tenantId, newUserId);
                db.UserMemberships.Add(membership);

                // Assign default role if specified
                if (!string.IsNullOrWhiteSpace(request.DefaultRole))
                {
                    var role = await db.Roles.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == request.DefaultRole, ct);

                    if (role != null)
                    {
                        var assignmentId = Guid.NewGuid().ToString();
                        var assignment = new MembershipRoleAssignment(assignmentId, tenantId, membershipId, role.Id);
                        db.MembershipRoleAssignments.Add(assignment);
                    }
                }
            }

            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/users/{newUserId}", new
            {
                userId = newUserId,
                message = "User created successfully"
            });
        });

        // PUT /api/v1/users/{userId} — update a user's profile
        // SuperAdmin: any organization | OrgAdmin/Admin: own organization | User: own profile only
        app.MapPut("/api/v1/users/{userId}", async (string userId, UpdateUserRequest request, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, IPasswordHasher? passwordHasher, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);
            var isOwnProfile = string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase);

            // Allow: SuperAdmin, OrgAdmin, or the user themselves
            if (!isSuperAdmin && !isAdmin && !isOwnProfile)
                return Results.Forbid();

            var userProfile = await db.UserProfiles.FindAsync([userId], ct);
            if (userProfile == null)
                return Results.NotFound();

            // If not super admin and not own profile, check if user is in same organization
            if (!isSuperAdmin && !isOwnProfile)
            {
                var tenantId = current.TenantId;
                var userInOrg = await db.UserMemberships
                    .AsNoTracking()
                    .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive, ct);

                if (!userInOrg)
                    return Results.Forbid();
            }

            // Update name
            if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
            {
                userProfile.UpdateName(
                    request.FirstName ?? userProfile.FirstName,
                    request.LastName ?? userProfile.LastName
                );
            }

            // Update email
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != userProfile.Email)
            {
                // Check if email is already taken
                var emailExists = await db.UserProfiles.AsNoTracking()
                    .AnyAsync(p => p.Email == request.Email && p.UserId != userId, ct);

                if (emailExists)
                    return Results.Conflict(new { message = "Email already in use" });

                userProfile.UpdateEmail(request.Email);
            }

            // Update profile image
            if (request.ProfileImageUrl != null)
                userProfile.UpdateProfileImage(request.ProfileImageUrl);

            // Update password (admins / super admins can set it; regular users use the dedicated change-password endpoint)
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (passwordHasher is null)
                    return Results.Problem(statusCode: 500, title: "Configuration error", detail: "Password hasher is not available.");

                var hashedPassword = passwordHasher.HashPassword(request.Password);
                userProfile.UpdatePassword(hashedPassword);
            }

            // Update other details
            userProfile.UpdateDetails(
                request.PhoneNumber,
                request.JobTitle,
                request.Bio,
                request.TimeZone,
                request.PreferredLanguage
            );

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "User profile updated successfully" });
        });

        // POST /api/v1/users/{userId}/activate — activate a user
        // SuperAdmin: all organizations | OrgAdmin: own organization only
        app.MapPost("/api/v1/users/{userId}/activate", async (string userId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            // Org admin can only activate users in their organization
            if (!isSuperAdmin)
            {
                var userInOrg = await db.UserMemberships
                    .AsNoTracking()
                    .AnyAsync(m => m.UserId == userId && m.TenantId == current.TenantId && m.IsActive, ct);

                if (!userInOrg)
                    return Results.Forbid();
            }

            var memberships = await db.UserMemberships
                .Where(m => m.UserId == userId)
                .ToListAsync(ct);

            if (memberships.Count == 0)
                return Results.NotFound();

            foreach (var membership in memberships)
                membership.SetActive(true);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "User activated successfully" });
        });

        // POST /api/v1/users/{userId}/deactivate — deactivate a user
        // SuperAdmin: all organizations | OrgAdmin: own organization only
        // Prevents self-deactivation
        app.MapPost("/api/v1/users/{userId}/deactivate", async (string userId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            // Prevent self-deactivation
            if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "You cannot deactivate your own account" });

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            // Org admin can only deactivate users in their organization
            if (!isSuperAdmin)
            {
                var userInOrg = await db.UserMemberships
                    .AsNoTracking()
                    .AnyAsync(m => m.UserId == userId && m.TenantId == current.TenantId && m.IsActive, ct);

                if (!userInOrg)
                    return Results.Forbid();
            }

            var memberships = await db.UserMemberships
                .Where(m => m.UserId == userId)
                .ToListAsync(ct);

            if (memberships.Count == 0)
                return Results.NotFound();

            foreach (var membership in memberships)
                membership.SetActive(false);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "User deactivated successfully" });
        });

        // DELETE /api/v1/users/{userId} — delete a user (SuperAdmin only, prevents deleting super admin)
        app.MapDelete("/api/v1/users/{userId}", async (string userId, HttpContext context, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);

            if (!isSuperAdmin)
                return Results.Forbid();

            // Prevent deleting super admin
            if (SuperAdmin.IsSuperAdmin(userId))
                return Results.BadRequest(new { message = "Cannot delete super admin" });

            var userProfile = await db.UserProfiles.FindAsync([userId], ct);
            if (userProfile == null)
                return Results.NotFound();

            // Delete memberships and role assignments
            var memberships = await db.UserMemberships
                .Where(m => m.UserId == userId)
                .ToListAsync(ct);

            var membershipIds = memberships.Select(m => m.Id).ToList();
            var roleAssignments = await db.MembershipRoleAssignments
                .Where(mra => membershipIds.Contains(mra.MembershipId))
                .ToListAsync(ct);

            db.MembershipRoleAssignments.RemoveRange(roleAssignments);
            db.UserMemberships.RemoveRange(memberships);
            db.UserProfiles.Remove(userProfile);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "User deleted successfully" });
        });

        // POST /api/v1/users/{userId}/organizations — add user to an organization
        // SuperAdmin: any organization | OrgAdmin: own organization only
        app.MapPost("/api/v1/users/{userId}/organizations", async (string userId, AddToOrganizationRequest request, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            // If org admin, can only add to their own organization
            if (!isSuperAdmin && request.TenantId != current.TenantId)
                return Results.Forbid();

            // Check if user already has membership in this organization
            var existingMembership = await db.UserMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == request.TenantId, ct);

            if (existingMembership != null)
                return Results.Conflict(new { message = "User already has membership in this organization" });

            var membershipId = Guid.NewGuid().ToString();
            var membership = new UserMembership(membershipId, request.TenantId, userId);
            db.UserMemberships.Add(membership);

            // Assign role if specified
            if (!string.IsNullOrWhiteSpace(request.RoleName))
            {
                var role = await db.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.TenantId == request.TenantId && r.Name == request.RoleName, ct);

                if (role != null)
                {
                    var assignmentId = Guid.NewGuid().ToString();
                    var assignment = new MembershipRoleAssignment(assignmentId, request.TenantId, membershipId, role.Id);
                    db.MembershipRoleAssignments.Add(assignment);
                }
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "User added to organization successfully" });
        });

        // DELETE /api/v1/users/{userId}/organizations/{tenantId} — remove user from an organization
        // SuperAdmin: all organizations | OrgAdmin: own organization only
        app.MapDelete("/api/v1/users/{userId}/organizations/{tenantId}", async (string userId, string tenantId, HttpContext context, ITenantContextAccessor ctx, OrganizationDbContext db, CancellationToken ct) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var currentUserId = RoleChecker.GetUserId(context.User);
            var isSuperAdmin = RoleChecker.IsSuperAdmin(currentUserId, context.User);
            var isAdmin = RoleChecker.IsAdmin(currentUserId, context.User);

            if (!isSuperAdmin && !isAdmin)
                return Results.Forbid();

            // If org admin, can only remove from their own organization
            if (!isSuperAdmin && tenantId != current.TenantId)
                return Results.Forbid();

            var membership = await db.UserMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId, ct);

            if (membership == null)
                return Results.NotFound();

            // Delete role assignments
            var roleAssignments = await db.MembershipRoleAssignments
                .Where(mra => mra.MembershipId == membership.Id)
                .ToListAsync(ct);

            db.MembershipRoleAssignments.RemoveRange(roleAssignments);
            db.UserMemberships.Remove(membership);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "User removed from organization successfully" });
        });
    }
}

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string[] TenantIds,
    string? DefaultRole = null,
    string? ProfileImageUrl = null
);

public record UpdateUserRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? ProfileImageUrl = null,
    string? PhoneNumber = null,
    string? JobTitle = null,
    string? Bio = null,
    string? TimeZone = null,
    string? PreferredLanguage = null,
    string? Password = null
);

public record AddToOrganizationRequest(
    string TenantId,
    string? RoleName = null
);