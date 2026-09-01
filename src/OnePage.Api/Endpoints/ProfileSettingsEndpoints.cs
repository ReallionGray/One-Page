using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;
using System.IO;

namespace OnePage.Api.Endpoints;

public static class ProfileSettingsEndpoints
{
    public static void MapProfileSettingsEndpoints(this WebApplication app)
    {
        // Get current user's profile
        app.MapGet("/api/v1/profile", async (HttpContext context, OrganizationDbContext db, CancellationToken ct) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var userId = user.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var userProfile = await db.UserProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (userProfile == null)
                return Results.NotFound();

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
                userProfile.PhoneNumber,
                userProfile.JobTitle,
                userProfile.Bio,
                userProfile.TimeZone,
                userProfile.PreferredLanguage,
                userProfile.CreatedAt,
                userProfile.UpdatedAt,
                organizations,
                roles
            });
        });

        // Update current user's profile
        app.MapPut("/api/v1/profile", async (UpdateProfileRequest request, HttpContext context, OrganizationDbContext db, CancellationToken ct) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var userId = user.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var userProfile = await db.UserProfiles.FindAsync([userId], ct);
            if (userProfile == null)
                return Results.NotFound();

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

            // Update other details
            userProfile.UpdateDetails(
                request.PhoneNumber,
                request.JobTitle,
                request.Bio,
                request.TimeZone,
                request.PreferredLanguage
            );

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Profile updated successfully" });
        });

        // Change password
        app.MapPost("/api/v1/profile/change-password", async (ChangePasswordRequest request, HttpContext context, OrganizationDbContext db, IPasswordHasher passwordHasher, CancellationToken ct) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var userId = user.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var userProfile = await db.UserProfiles.FindAsync([userId], ct);
            if (userProfile == null)
                return Results.NotFound();

            // Verify current password
            if (!string.IsNullOrEmpty(userProfile.PasswordHash))
            {
                if (!passwordHasher.VerifyPassword(request.CurrentPassword, userProfile.PasswordHash))
                    return Results.BadRequest(new { message = "Incorrect current password" });
            }
            else
            {
                // Seeded user fallback
                var email = userProfile.Email;
                var expected = email.ToLower() switch
                {
                    "superadmin@demo.com" => "SuperAdmin@123!",
                    "admin@demo.com" => "Admin@123!",
                    "hrmanager@demo.com" => "HRManager@123!",
                    "accountant@demo.com" => "Accountant@123!",
                    "sales@demo.com" => "Sales@123!",
                    "user@demo.com" => "User@123!",
                    _ => null
                };

                if (expected != null && request.CurrentPassword != expected)
                    return Results.BadRequest(new { message = "Incorrect current password" });
            }

            // Update with new password
            var newHash = passwordHasher.HashPassword(request.NewPassword);
            userProfile.UpdatePassword(newHash);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Password changed successfully" });
        });

        // Upload profile image — saves the file to disk and updates the user profile
        app.MapPost("/api/v1/profile/upload-image", async (HttpContext httpContext, OrganizationDbContext db, CancellationToken ct) =>
        {
            var user = httpContext.User;
            if (user.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var userId = user.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            // Read the form to obtain the uploaded file
            var form = await httpContext.Request.ReadFormAsync(ct);
            var file = form.Files.FirstOrDefault();

            if (file is null || file.Length == 0)
                return Results.BadRequest(new { message = "No file provided." });

            // Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            if (!allowedExtensions.Contains(extension))
                return Results.BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp." });

            // Validate file size (max 5 MB)
            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length > maxBytes)
                return Results.BadRequest(new { message = $"File is too large. Maximum size is {maxBytes / 1024 / 1024} MB." });

            // Create the uploads directory if it doesn't exist
            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsRoot);

            // Generate a unique filename to avoid collisions
            var safeFileName = $"{userId}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsRoot, safeFileName);

            // Save the file to disk
            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Build the public URL for the uploaded file
            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var imageUrl = $"{baseUrl}/uploads/profiles/{safeFileName}";

            // Update the user's profile with the new image URL
            var userProfile = await db.UserProfiles.FindAsync([userId], ct);
            if (userProfile is null)
                return Results.NotFound();

            userProfile.UpdateProfileImage(imageUrl);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                message = "Profile image uploaded successfully.",
                imageUrl,
                fileSize = file.Length,
                contentType = file.ContentType
            });
        });
    }
}

public record UpdateProfileRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? ProfileImageUrl = null,
    string? PhoneNumber = null,
    string? JobTitle = null,
    string? Bio = null,
    string? TimeZone = null,
    string? PreferredLanguage = null
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);