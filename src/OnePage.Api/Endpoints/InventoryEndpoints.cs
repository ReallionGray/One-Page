using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/inventory/items", async (CreateInventoryItemCommand c, ITenantContextAccessor ctx, IEntitlementEvaluator ent, IAuthorizationEvaluator auth, IInventoryRepository inventory, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Inventory).Allowed) return Results.Problem(statusCode: 403, title: "Inventory module unavailable", detail: "The Inventory module is not enabled.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.InventoryAdjust));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to create inventory items.");
            var item = await inventory.CreateAsync(new InventoryItem(c.Id, current.TenantId, c.Sku, c.Name, c.Quantity), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.create:{item.Id}", "inventory", item.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/inventory/items/{item.Id}", item);
        });

        app.MapPost("/api/v1/inventory/items/{id}/adjust", async (string id, AdjustInventoryCommand c, ITenantContextAccessor ctx, IEntitlementEvaluator ent, IAuthorizationEvaluator auth, IInventoryRepository inventory, IApprovalRepository approvals, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Inventory).Allowed) return Results.Problem(statusCode: 403, title: "Inventory module unavailable", detail: "The Inventory module is not enabled.");
            var item = await inventory.GetAsync(id, ct);
            if (item is null) return Results.NotFound();
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.InventoryAdjust, new AuthorizationScope(null, null, null, null)));
            if (!decision.Allowed)
            {
                var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "inventory.adjust", item.Id, current.UserId, c.Delta.ToString()));
                await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.adjust.request:{item.Id}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);
                return Results.Accepted($"/api/v1/approvals/{req.Id}", new { approvalId = req.Id });
            }
            item.Adjust(c.Delta);
            await inventory.UpdateAsync(item, ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.adjust:{item.Id}", "inventory", item.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(item);
        });
    }
}
