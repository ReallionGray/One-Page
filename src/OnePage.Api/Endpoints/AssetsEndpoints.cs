using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class AssetsEndpoints
{
    public static void MapAssetsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/assets", async (CreateAssetCommand c, ITenantContextAccessor ctx, IAuthorizationEvaluator auth, IEntitlementEvaluator ent, IAssetsRepository repo, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Assets).Allowed) return Results.Problem(statusCode: 403, title: "Assets module unavailable", detail: "The Assets module is not enabled for this subscription.");
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.AssetCreate, new AuthorizationScope(c.LegalEntityId, c.BranchId, c.DepartmentId, c.LocationId)));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to create assets.", extensions: new Dictionary<string, object?> { ["denialReason"] = decision.DenialReason?.ToString() });
            var asset = await repo.CreateAsync(new Asset(c.Id, current.TenantId, c.Tag, c.Name, c.Description, c.LocationId, c.CustodianEmployeeId), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.create:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/assets/{asset.Id}", asset);
        });

        app.MapGet("/api/v1/assets/{id}", async (string id, ITenantContextAccessor ctx, IAuthorizationEvaluator auth, IEntitlementEvaluator ent, IAssetsRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var asset = await repo.GetAsync(id, ct);
            if (asset is null) return Results.NotFound();
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.AssetView, new AuthorizationScope(null, null, null, asset.LocationId)));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to view asset.", extensions: new Dictionary<string, object?> { ["denialReason"] = decision.DenialReason?.ToString() });
            return Results.Ok(asset);
        });

        app.MapPost("/api/v1/assets/{id}/assign", async (string id, AssignAssetCommand c, ITenantContextAccessor ctx, IAuthorizationEvaluator auth, IEntitlementEvaluator ent, IAssetsRepository repo, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Assets).Allowed) return Results.Problem(statusCode: 403, title: "Assets module unavailable", detail: "The Assets module is not enabled for this subscription.");
            var asset = await repo.GetAsync(id, ct);
            if (asset is null) return Results.NotFound();
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.AssetAssign, new AuthorizationScope(null, null, null, asset.LocationId)));
            if (!decision.Allowed) return Results.Problem(statusCode: 403, title: "Authorization denied", detail: "Not authorized to assign asset.", extensions: new Dictionary<string, object?> { ["denialReason"] = decision.DenialReason?.ToString() });
            asset.AssignToEmployee(c.EmployeeId);
            await repo.UpdateAsync(asset, ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.assign:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(asset);
        });

        app.MapPost("/api/v1/assets/{id}/dispose", async (string id, DisposeAssetCommand c, ITenantContextAccessor ctx, IAuthorizationEvaluator auth, IEntitlementEvaluator ent, IAssetsRepository repo, IAuditRepository audit, IApprovalRepository approvals, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            if (!ent.Evaluate(current, EntitlementKeys.Modules.Assets).Allowed) return Results.Problem(statusCode: 403, title: "Assets module unavailable", detail: "The Assets module is not enabled for this subscription.");
            var asset = await repo.GetAsync(id, ct);
            if (asset is null) return Results.NotFound();
            var decision = await auth.AuthorizeAsync(new AuthorizationRequest(current, PermissionCatalog.AssetDispose, new AuthorizationScope(null, null, null, asset.LocationId)));
            if (!decision.Allowed)
            {
                // Create approval request for disposal
                var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "asset.dispose", asset.Id, current.UserId, c.Reason), ct);
                await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.dispose.request:{asset.Id}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);
                return Results.Accepted($"/api/v1/approvals/{req.Id}", new { approvalId = req.Id });
            }
            asset.Dispose(current.UserId);
            await repo.UpdateAsync(asset, ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.dispose:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(asset);
        });
    }
}
