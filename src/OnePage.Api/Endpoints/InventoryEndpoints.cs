using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/inventory/items", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IInventoryRepository inventory, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check subscription only (no specific permission required for listing)
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Inventory, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            
            var list = await inventory.ListAsync(current.TenantId, ct);
            return Results.Ok(list);
        });

        app.MapPost("/api/v1/inventory/items", async (CreateInventoryItemCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IInventoryRepository inventory, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Inventory, 
                PermissionCatalog.InventoryAdjust,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var item = await inventory.CreateAsync(new InventoryItem(c.Id, current.TenantId, c.Sku, c.Name, c.Quantity), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.create:{item.Id}", "inventory", item.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/inventory/items/{item.Id}", item);
        });

        app.MapPost("/api/v1/inventory/items/{id}/adjust", async (string id, AdjustInventoryCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IInventoryRepository inventory, IApprovalRepository approvals, IWorkflowRepository workflows, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var item = await inventory.GetAsync(id, ct);
            if (item is null) return Results.NotFound();
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Inventory, 
                PermissionCatalog.InventoryAdjust, 
                new AuthorizationScope(null, null, null, null),
                cancellationToken: ct);
            
            if (accessResult is not null)
            {
                var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "inventory.adjust", item.Id, current.UserId, c.Delta.ToString()));
                // Resolve and attach a matching workflow definition if one exists.
                var workflow = await workflows.FindMatchingAsync(current.TenantId, "inventory.adjust", null, Math.Abs(c.Delta), ct);
                if (workflow is not null)
                {
                    req.AttachWorkflow(workflow.Id);
                    await approvals.UpdateAsync(req, ct);
                }
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
