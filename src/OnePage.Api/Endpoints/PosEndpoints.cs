using OnePage.Platform;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace OnePage.Api.Endpoints;

public static class PosEndpoints
{
    public static void MapPosEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/pos/sales", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPosRepository pos, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check subscription only (no specific permission required for listing)
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Pos, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            
            var list = await pos.ListAsync(current.TenantId, ct);
            return Results.Ok(list);
        });

        app.MapPost("/api/v1/pos/sales", async (CreatePosSaleCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPosRepository pos, IInventoryRepository inventory, IApprovalRepository approvals, IAuditRepository audit, OrganizationDbContext db, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Pos, 
                PermissionCatalog.PosSaleCreate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;

            // Begin transaction so sale + inventory adjustments/approvals/audits are atomic for demo
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // serialize lines into the stored sale record for audit/history
                var linesJson = c.Lines is null ? null : JsonSerializer.Serialize(c.Lines);
                var sale = await pos.CreateAsync(new PosSale(c.Id, current.TenantId, c.RegisterId, c.Total, linesJson), ct);

                // include sale details in audit after-json
                var saleJson = JsonSerializer.Serialize(sale);
                await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"pos.sale.create:{sale.Id}", "pos", sale.Id, null, saleJson, current.CorrelationId, null, null), ct);

                // For each sale line, attempt inventory adjustment or create approval if user lacks permission
                if (c.Lines is not null)
                {
                    // Check inventory adjustment permission separately for each line
                    var invAccessResult = await ctx.RequireModuleAccess(
                        moduleAccess, 
                        EntitlementKeys.Modules.Inventory, 
                        PermissionCatalog.InventoryAdjust,
                        cancellationToken: ct);
                    
                    var hasInventoryPermission = invAccessResult is null;

                    foreach (var line in c.Lines)
                    {
                        var item = await inventory.GetBySkuForUpdateAsync(current.TenantId, line.Sku, ct);
                        if (item is null)
                        {
                            // log missing inventory SKU
                            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.missing:{line.Sku}", "inventory", null, null, JsonSerializer.Serialize(new { Sku = line.Sku, Quantity = line.Quantity }), current.CorrelationId, null, null), ct);
                            continue;
                        }

                        if (hasInventoryPermission)
                        {
                            // enforce availability for demo: prevent negative stock
                            if (item.Quantity < line.Quantity)
                            {
                                return Results.Problem(statusCode: 400, title: "Insufficient stock", detail: $"Not enough stock for SKU {line.Sku}");
                            }

                            var before = JsonSerializer.Serialize(item);
                            item.Adjust(-line.Quantity);
                            await inventory.UpdateAsync(item, ct);
                            var after = JsonSerializer.Serialize(item);
                            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.adjust:{item.Id}", "inventory", item.Id, before, after, current.CorrelationId, null, null), ct);
                        }
                        else
                        {
                            var req = await approvals.CreateAsync(new OnePage.Platform.ApprovalRequest(Guid.NewGuid().ToString("N"), current.TenantId, "inventory.adjust", item.Id, current.UserId, line.Quantity.ToString()));
                            await audit.AddAsync(new OnePage.Platform.AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"inventory.adjust.request:{item.Id}", "approval", req.Id, JsonSerializer.Serialize(new { Sku = line.Sku, Quantity = line.Quantity }), null, current.CorrelationId, null, null), ct);
                        }
                    }
                }

                await tx.CommitAsync(ct);
                return Results.Created($"/api/v1/pos/sales/{c.Id}", sale);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
