using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinanceEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/finance/journal-entries", async (CreateJournalEntryCommand c, ITenantContextAccessor ctx, IEntitlementEvaluator ent, IAuthorizationEvaluator auth, IFinanceRepository finance, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Finance).Allowed) return Results.Problem(statusCode: 403, title: "Finance module unavailable", detail: "The Finance module is not enabled.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.FinanceJournalExport));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to create journal entries.");
            var entry = await finance.CreateAsync(new JournalEntry(c.Id, current.TenantId, c.Reference), ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"finance.journal.create:{entry.Id}", "finance", entry.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/finance/journal-entries/{entry.Id}", entry);
        });

        app.MapGet("/api/v1/finance/journal-entries", async (ITenantContextAccessor ctx, IFinanceRepository finance, IAuthorizationEvaluator auth, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.FinanceJournalExport));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to export journals.");
            var list = await finance.ListAsync(current.TenantId, ct);
            return Results.Ok(list);
        });
    }
}
