using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/reporting/run", async (string report, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription (feature) and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Features.AdvancedReporting, 
                PermissionCatalog.ReportRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"report.run:{report}", "reporting", null, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(new { report, status = "queued" });
        });
    }
}
