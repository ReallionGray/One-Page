using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class ProcurementEndpoints
{
    public static void MapProcurementEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/procurement/purchase-orders", async (CreatePurchaseOrderCommand c, ITenantContextAccessor ctx, IEntitlementEvaluator ent, IAuthorizationEvaluator auth, IProcurementRepository procurement, IApprovalRepository approvals, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Procurement).Allowed) return Results.Problem(statusCode: 403, title: "Procurement module unavailable", detail: "The Procurement module is not enabled.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.PurchaseOrderCreate));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to create purchase orders.");
            var po = await procurement.CreateAsync(new PurchaseOrder(c.Id, current.TenantId, c.Supplier, c.TotalAmount), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"purchase_order.create:{po.Id}", "procurement", po.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/procurement/purchase-orders/{po.Id}", po);
        });

        app.MapPost("/api/v1/procurement/purchase-orders/{id}/approve", async (string id, ITenantContextAccessor ctx, IAuthorizationEvaluator auth, IApprovalRepository approvals, IProcurementRepository procurement, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var po = await procurement.GetAsync(id, ct);
            if (po is null) return Results.NotFound();
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.PurchaseOrderApprove, new AuthorizationScope(null, null, null, null), po.TotalAmount));
            if (!decision.Allowed)
            {
                var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "purchase_order.approve", po.Id, current.UserId, po.TotalAmount.ToString()));
                await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"purchase_order.approve.request:{po.Id}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);
                return Results.Accepted($"/api/v1/approvals/{req.Id}", new { approvalId = req.Id });
            }
            po.Approve(current.UserId);
            await procurement.UpdateAsync(po, ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"purchase_order.approve:{po.Id}", "procurement", po.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(po);
        });
    }
}
