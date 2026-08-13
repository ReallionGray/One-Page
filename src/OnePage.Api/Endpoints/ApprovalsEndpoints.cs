using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class ApprovalsEndpoints
{
    public static void MapApprovalEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/approvals/{id}", async (string id, ITenantContextAccessor ctx, IApprovalRepository approvals, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var req = await approvals.GetAsync(id, ct);
            if (req is null) return Results.NotFound();
            if (!string.Equals(req.TenantId, current.TenantId, StringComparison.Ordinal)) return Results.Problem(statusCode: 403, title: "Cross-tenant", detail: "Request does not belong to current tenant.");
            return Results.Ok(req);
        });

        app.MapPost("/api/v1/approvals/{id}/decide", async (string id, DecideApprovalCommand c, ITenantContextAccessor ctx, IApprovalRepository approvals, IAuthorizationEvaluator auth, IEntitlementEvaluator ent, IAuditRepository audit, IAssetsRepository assets, IProcurementRepository procurement, IInventoryRepository inventory, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var req = await approvals.GetAsync(id, ct);
            if (req is null) return Results.NotFound();
            if (req.TenantId != current.TenantId) return Results.Problem(statusCode: 403, title: "Cross-tenant", detail: "Request does not belong to current tenant.");
            var decisionAuth = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.ApprovalReview));
            if (!decisionAuth.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to decide approvals.");
            try
            {
                if (c.Approve)
                {
                    req.Approve(current.UserId, c.Comment);
                    await approvals.UpdateAsync(req, ct);
                    await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"approval.approve:{req.Id}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);
                    if (req.ResourceType == "asset.dispose" && !string.IsNullOrWhiteSpace(req.ResourceId))
                    {
                        var asset = await assets.GetAsync(req.ResourceId, ct);
                        if (asset is not null)
                        {
                            asset.Dispose(current.UserId);
                            await assets.UpdateAsync(asset, ct);
                            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.dispose:approved:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
                        }
                    }
                    if (req.ResourceType == "purchase_order.approve" && !string.IsNullOrWhiteSpace(req.ResourceId))
                    {
                        var po = await procurement.GetAsync(req.ResourceId, ct);
                        if (po is not null)
                        {
                            po.Approve(current.UserId);
                            await procurement.UpdateAsync(po, ct);
                            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"purchase_order.approve:approved:{po.Id}", "procurement", po.Id, null, null, current.CorrelationId, null, null), ct);
                        }
                    }
                    if (req.ResourceType == "inventory.adjust" && !string.IsNullOrWhiteSpace(req.ResourceId))
                    {
                        var item = await inventory.GetAsync(req.ResourceId, ct);
                        if (item is not null)
                        {
                            // reason contains delta encoded as string in this simplified model
                            if (decimal.TryParse(req.Reason, out var delta))
                            {
                                item.Adjust(delta);
                                await inventory.UpdateAsync(item, ct);
                                await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.adjust:approved:{item.Id}", "inventory", item.Id, null, null, current.CorrelationId, null, null), ct);
                            }
                        }
                    }
                    return Results.Ok(req);
                }
                else
                {
                    req.Reject(current.UserId, c.Comment);
                    await approvals.UpdateAsync(req, ct);
                    await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"approval.reject:{req.Id}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);
                    return Results.Ok(req);
                }
            }
            catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "Invalid approval decision", detail: ex.Message); }
        });

        app.MapGet("/api/v1/platform/audit/export", async (ITenantContextAccessor ctx, IAuditRepository audit, IAuthorizationEvaluator auth, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.ReportExport));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to export audit logs.");
            var events = await audit.ExportTenantEventsAsync(current.TenantId, ct);
            return Results.Ok(events);
        });
    }
}
