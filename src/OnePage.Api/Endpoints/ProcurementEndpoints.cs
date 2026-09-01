using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class ProcurementEndpoints
{
    public static void MapProcurementEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/procurement/purchase-orders", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IProcurementRepository procurement, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check subscription only (no specific permission required for listing)
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Procurement, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            
            var list = await procurement.ListAsync(current.TenantId, ct);
            return Results.Ok(list);
        });

        app.MapPost("/api/v1/procurement/purchase-orders", async (CreatePurchaseOrderCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IProcurementRepository procurement, IApprovalRepository approvals, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Procurement, 
                PermissionCatalog.PurchaseOrderCreate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var po = await procurement.CreateAsync(new PurchaseOrder(c.Id, current.TenantId, c.Supplier, c.TotalAmount), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"purchase_order.create:{po.Id}", "procurement", po.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/procurement/purchase-orders/{po.Id}", po);
        });

        app.MapPost("/api/v1/procurement/purchase-orders/{id}/approve", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IApprovalRepository approvals, IWorkflowRepository workflows, IProcurementRepository procurement, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var po = await procurement.GetAsync(id, ct);
            if (po is null) return Results.NotFound();
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Procurement, 
                PermissionCatalog.PurchaseOrderApprove, 
                new AuthorizationScope(null, null, null, null), 
                po.TotalAmount,
                cancellationToken: ct);
            
            if (accessResult is not null)
            {
                var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "purchase_order.approve", po.Id, current.UserId, po.TotalAmount.ToString()));
                // Resolve and attach a matching workflow definition if one exists.
                var workflow = await workflows.FindMatchingAsync(current.TenantId, "purchase_order.approve", po.TotalAmount, null, ct);
                if (workflow is not null)
                {
                    req.AttachWorkflow(workflow.Id);
                    await approvals.UpdateAsync(req, ct);
                }
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
