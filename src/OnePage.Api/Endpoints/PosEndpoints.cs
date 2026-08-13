using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class PosEndpoints
{
    public static void MapPosEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/pos/sales", async (CreatePosSaleCommand c, ITenantContextAccessor ctx, IEntitlementEvaluator ent, IAuthorizationEvaluator auth, IPosRepository pos, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Pos).Allowed) return Results.Problem(statusCode: 403, title: "POS module unavailable", detail: "The POS module is not enabled.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.PosSaleCreate));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to create POS sales.");
            var sale = await pos.CreateAsync(new PosSale(c.Id, current.TenantId, c.RegisterId, c.Total), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"pos.sale.create:{sale.Id}", "pos", sale.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/pos/sales/{sale.Id}", sale);
        });
    }
}
