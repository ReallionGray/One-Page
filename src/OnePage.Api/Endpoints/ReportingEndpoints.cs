using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/reporting/run", async (string report, ITenantContextAccessor ctx, IAuthorizationEvaluator auth, IEntitlementEvaluator ent, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Features.AdvancedReporting).Allowed) return Results.Problem(statusCode: 403, title: "Reporting feature unavailable", detail: "Advanced reporting feature is not enabled.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.ReportRun));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to run reports.");
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"report.run:{report}", "reporting", null, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(new { report, status = "queued" });
        });
    }
}
