using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class AssetsEndpoints
{
    public static void MapAssetsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/assets", async (CreateAssetCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IAssetsRepository repo, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Assets, 
                PermissionCatalog.AssetCreate, 
                new AuthorizationScope(c.LegalEntityId, c.BranchId, c.DepartmentId, c.LocationId),
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var asset = await repo.CreateAsync(new Asset(c.Id, current.TenantId, c.Tag, c.Name, c.Description, c.LocationId, c.CustodianEmployeeId), ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.create:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Created($"/api/v1/assets/{asset.Id}", asset);
        });

        app.MapGet("/api/v1/assets/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IAssetsRepository repo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var asset = await repo.GetAsync(id, ct);
            if (asset is null) return Results.NotFound();
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Assets, 
                PermissionCatalog.AssetView, 
                new AuthorizationScope(null, null, null, asset.LocationId),
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            return Results.Ok(asset);
        });

        app.MapPost("/api/v1/assets/{id}/assign", async (string id, AssignAssetCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IAssetsRepository repo, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var asset = await repo.GetAsync(id, ct);
            if (asset is null) return Results.NotFound();
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Assets, 
                PermissionCatalog.AssetAssign, 
                new AuthorizationScope(null, null, null, asset.LocationId),
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            asset.AssignToEmployee(c.EmployeeId);
            await repo.UpdateAsync(asset, ct);
            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"asset.assign:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(asset);
        });

        app.MapPost("/api/v1/assets/{id}/dispose", async (string id, DisposeAssetCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IAssetsRepository repo, IAuditRepository audit, IApprovalRepository approvals, IWorkflowRepository workflows, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var asset = await repo.GetAsync(id, ct);
            if (asset is null) return Results.NotFound();
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Assets, 
                PermissionCatalog.AssetDispose, 
                new AuthorizationScope(null, null, null, asset.LocationId),
                cancellationToken: ct);
            
            if (accessResult is not null)
            {
                // If access is denied due to permissions, create approval request for disposal
                var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "asset.dispose", asset.Id, current.UserId, c.Reason), ct);
                // Resolve and attach a matching workflow definition if one exists.
                var workflow = await workflows.FindMatchingAsync(current.TenantId, "asset.dispose", null, null, ct);
                if (workflow is not null)
                {
                    req.AttachWorkflow(workflow.Id);
                    await approvals.UpdateAsync(req, ct);
                }
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
