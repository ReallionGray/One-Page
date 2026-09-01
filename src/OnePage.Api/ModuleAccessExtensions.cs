using Microsoft.AspNetCore.Http;
using OnePage.Platform;

namespace OnePage.Api;

// Extension methods for easier use in endpoints
public static class ModuleAccessExtensions
{
    public static async Task<IResult?> RequireModuleAccess(
        this ITenantContextAccessor contextAccessor,
        IModuleAccessEvaluator evaluator,
        EntitlementKey? moduleKey,
        PermissionKey? requiredPermission = null,
        AuthorizationScope? scope = null,
        decimal? amount = null,
        CancellationToken cancellationToken = default)
    {
        var current = contextAccessor.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
        
        // If no module key is provided, only check permission
        if (moduleKey is null)
        {
            if (requiredPermission is null)
            {
                return null; // No restrictions
            }
            
            // Use a dummy module key for permission-only check
            var dummyModuleKey = EntitlementKey.Module("system");
            var permissionDecision = await evaluator.EvaluateModuleAccessAsync(
                current, 
                dummyModuleKey, 
                requiredPermission, 
                scope, 
                amount, 
                cancellationToken);
            
            if (!permissionDecision.Allowed)
            {
                return Results.Problem(
                    statusCode: 403, 
                    title: "Permission required", 
                    detail: $"You lack the required permission: {permissionDecision.RequiredPermission}",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "MissingPermission",
                        ["requiredPermission"] = permissionDecision.RequiredPermission
                    });
            }
            
            return null;
        }
        
        var moduleDecision = await evaluator.EvaluateModuleAccessAsync(
            current, 
            moduleKey.Value, 
            requiredPermission, 
            scope, 
            amount, 
            cancellationToken);

        if (!moduleDecision.Allowed)
        {
            var title = moduleDecision.DenialReason switch
            {
                ModuleAccessDenialReason.SubscriptionRequired => "Subscription required",
                ModuleAccessDenialReason.SubscriptionInactive => "Subscription inactive",
                ModuleAccessDenialReason.PermissionRequired => "Permission required",
                ModuleAccessDenialReason.SubscriptionAndPermissionRequired => "Subscription and permission required",
                _ => "Access denied"
            };

            var moduleName = moduleKey!.Value.Name.ToUpperInvariant();
            var detail = moduleDecision.DenialReason switch
            {
                ModuleAccessDenialReason.SubscriptionRequired => $"{moduleName} module unavailable. Your current plan ({moduleDecision.SubscriptionPlan}) does not include access to this module.",
                ModuleAccessDenialReason.SubscriptionInactive => $"{moduleName} module unavailable. Your subscription is not active.",
                ModuleAccessDenialReason.PermissionRequired => $"You lack the required permission: {moduleDecision.RequiredPermission}",
                ModuleAccessDenialReason.SubscriptionAndPermissionRequired => $"{moduleName} module unavailable. You need both an active subscription and the permission: {moduleDecision.RequiredPermission}",
                _ => "Access to this module is denied."
            };

            return Results.Problem(
                statusCode: 403, 
                title: title, 
                detail: detail,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = moduleDecision.DenialReason == ModuleAccessDenialReason.PermissionRequired || moduleDecision.DenialReason == ModuleAccessDenialReason.SubscriptionAndPermissionRequired
                        ? "MissingPermission"
                        : moduleDecision.DenialReason?.ToString(),
                    ["subscriptionPlan"] = moduleDecision.SubscriptionPlan,
                    ["requiredPermission"] = moduleDecision.RequiredPermission
                });
        }

        return null; // Indicates success - caller should continue with endpoint logic
    }
}
