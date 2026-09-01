using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinanceEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/finance/journal-entries", async (CreateJournalEntryCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IFinanceRepository finance, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Finance, 
                PermissionCatalog.FinanceJournalExport,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var entry = await finance.CreateAsync(new JournalEntry(c.Id, current.TenantId, c.Reference), ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"finance.journal.create:{entry.Id}", "finance", entry.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/finance/journal-entries/{entry.Id}", entry);
        });

        app.MapGet("/api/v1/finance/journal-entries", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IFinanceRepository finance, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Finance, 
                PermissionCatalog.FinanceJournalExport,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var list = await finance.ListAsync(current.TenantId, ct);
            return Results.Ok(list);
        });
    }
}
