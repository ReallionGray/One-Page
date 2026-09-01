using OnePage.Platform;
using Microsoft.AspNetCore.Builder;
using System.Security.Claims;

namespace OnePage.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        // Get available organizations for a user (by email/username)
        app.MapPost("/api/v1/auth/organizations", async (OrganizationLookupRequest request, IHRRepository hrRepository, ITenantRepository tenants, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.Problem(statusCode: 400, title: "Bad Request", detail: "Email is required");

            var users = await hrRepository.GetByEmailAcrossAllTenantsAsync(request.Email, ct);
            if (users.Count == 0)
                return Results.Problem(statusCode: 404, title: "User not found", detail: "No user found with this email");

            var tenantIds = users.Select(u => u.TenantId).Distinct().ToArray();
            var tenantRecords = await tenants.ListByIdsAsync(tenantIds, ct);
            var names = tenantRecords.ToDictionary(t => t.Id, t => t.Name, StringComparer.OrdinalIgnoreCase);

            var organizations = users.Select(u => new
            {
                tenantId = u.TenantId,
                tenantName = names.TryGetValue(u.TenantId, out var name) ? name : u.TenantId,
                userId = u.Id,
                email = u.Email,
                firstName = u.FirstName,
                lastName = u.LastName
            }).ToList();

            return Results.Ok(new
            {
                organizations,
                requiresSelection = organizations.Count > 1
            });
        });

        // Login endpoint - authenticate with username/password.
        // Tenant is optional: a single membership is selected automatically;
        // multiple memberships return the organization list for the client to choose.
        app.MapPost("/api/v1/auth/login", async (LoginRequest request, IAuthenticationService authService, CancellationToken ct) =>
        {
            var result = await authService.AuthenticateAsync(request, ct);

            if (result.RequiresOrganizationSelection)
            {
                return Results.Ok(new
                {
                    requiresOrganizationSelection = true,
                    organizations = result.Organizations ?? Array.Empty<OrganizationOption>()
                });
            }
            
            if (!result.Success)
            {
                return Results.Problem(
                    statusCode: result.FailureReason switch
                    {
                        AuthenticationFailureReason.InvalidCredentials => StatusCodes.Status401Unauthorized,
                        AuthenticationFailureReason.UserNotFound => StatusCodes.Status401Unauthorized,
                        AuthenticationFailureReason.UserInactive => StatusCodes.Status403Forbidden,
                        AuthenticationFailureReason.TenantNotFound => StatusCodes.Status401Unauthorized,
                        _ => StatusCodes.Status401Unauthorized
                    },
                    title: "Authentication failed",
                    detail: result.ErrorMessage ?? "Invalid credentials",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.FailureReason?.ToString(),
                        ["failureReason"] = result.FailureReason?.ToString()
                    });
            }
            
            return Results.Ok(result.Tokens);
        });

        // Refresh token endpoint
        app.MapPost("/api/v1/auth/refresh", async (RefreshTokenRequest request, IAuthenticationService authService, CancellationToken ct) =>
        {
            var result = await authService.RefreshAsync(request, ct);
            
            if (!result.Success)
            {
                return Results.Problem(
                    statusCode: result.FailureReason switch
                    {
                        AuthenticationFailureReason.InvalidToken => StatusCodes.Status401Unauthorized,
                        AuthenticationFailureReason.TokenExpired => StatusCodes.Status401Unauthorized,
                        AuthenticationFailureReason.UserInactive => StatusCodes.Status403Forbidden,
                        _ => StatusCodes.Status401Unauthorized
                    },
                    title: "Refresh failed",
                    detail: result.ErrorMessage ?? "Invalid refresh token",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.FailureReason?.ToString(),
                        ["failureReason"] = result.FailureReason?.ToString()
                    });
            }
            
            return Results.Ok(result.Tokens);
        });

        // Validate token endpoint
        app.MapGet("/api/v1/auth/validate", async (HttpContext context, IAuthenticationService authService, CancellationToken ct) =>
        {
            // Get token from Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(statusCode: 401, title: "Unauthorized", detail: "Bearer token required");
            }
            
            var token = authHeader["Bearer ".Length..].Trim();
            var result = await authService.ValidateTokenAsync(token, ct);
            
            if (!result.Success)
            {
                return Results.Problem(
                    statusCode: 401,
                    title: "Invalid token",
                    detail: result.ErrorMessage ?? "Token validation failed",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = result.FailureReason?.ToString()
                    });
            }
            
            // Return principal information (without sensitive data)
            return Results.Ok(new
            {
                valid = true,
                userId = result.Principal?.Id,
                username = result.Principal?.Username,
                roles = result.Principal?.Roles,
                permissions = result.Principal?.Permissions,
                isActive = result.Principal?.IsActive
            });
        });

        // Logout endpoint (clears client-side token)
        app.MapPost("/api/v1/auth/logout", () =>
        {
            // JWT tokens are stateless - client should discard the token
            // For session-based auth, we would invalidate the session here
            return Results.Ok(new { message = "Logged out successfully" });
        });

        // Get current user info
        app.MapGet("/api/v1/auth/me", (HttpContext context) =>
        {
            var user = context.User;
            
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Problem(statusCode: 401, title: "Unauthorized", detail: "Not authenticated");
            }
            
            return Results.Ok(new
            {
                userId = user.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                username = user.Claims.FirstOrDefault(c => c.Type == "username")?.Value ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray(),
                permissions = user.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToArray(),
                claims = user.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        });

        // Get accessible modules for current user
        app.MapGet("/api/v1/modules/accessible", async (HttpContext context, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccessEvaluator, CancellationToken ct) =>
        {
            var user = context.User;
            
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Problem(statusCode: 401, title: "Unauthorized", detail: "Not authenticated");
            }

            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var tenantId = current.TenantId;
            var userId = user.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Define all available modules with their entitlement keys
            var allModules = new[]
            {
                new { key = "assets", label = "Assets", icon = "📦", entitlementKey = EntitlementKeys.Modules.Assets, permission = (PermissionKey?)null },
                new { key = "approvals", label = "Approvals", icon = "✅", entitlementKey = EntitlementKeys.Modules.Assets, permission = (PermissionKey?)null }, // approvals is part of assets
                new { key = "workflowSetup", label = "Workflow Setup", icon = "⚙️", entitlementKey = EntitlementKeys.Modules.Procurement, permission = (PermissionKey?)PermissionCatalog.WorkflowManage },
                new { key = "employees", label = "Employees", icon = "👥", entitlementKey = EntitlementKeys.Modules.Hr, permission = (PermissionKey?)null },
                new { key = "purchaseOrders", label = "Purchase Orders", icon = "🛒", entitlementKey = EntitlementKeys.Modules.Procurement, permission = (PermissionKey?)null },
                new { key = "inventory", label = "Inventory", icon = "🏭", entitlementKey = EntitlementKeys.Modules.Inventory, permission = (PermissionKey?)null },
                new { key = "payroll", label = "Payroll", icon = "💰", entitlementKey = EntitlementKeys.Modules.Payroll, permission = (PermissionKey?)null },
                new { key = "posSales", label = "POS Sales", icon = "💳", entitlementKey = EntitlementKeys.Modules.Pos, permission = (PermissionKey?)null },
                new { key = "finance", label = "Finance", icon = "📊", entitlementKey = EntitlementKeys.Modules.Finance, permission = (PermissionKey?)null },
                new { key = "reporting", label = "Reporting", icon = "📈", entitlementKey = EntitlementKeys.Modules.Reporting, permission = (PermissionKey?)null }
            };

            var accessibleModules = new List<object>();

            foreach (var module in allModules)
            {
                var decision = await moduleAccessEvaluator.EvaluateModuleAccessAsync(
                    current, 
                    module.entitlementKey, 
                    module.permission, 
                    null, 
                    null, 
                    ct);

                if (decision.Allowed)
                {
                    accessibleModules.Add(new
                    {
                        key = module.key,
                        label = module.label,
                        icon = module.icon
                    });
                }
            }

            return Results.Ok(new
            {
                modules = accessibleModules,
                tenantId
            });
        });
    }
}
