using OnePage.Platform;
using OnePage.Hr;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class DisciplinaryEndpoints
{
    public static void MapDisciplinaryEndpoints(this WebApplication app)
    {
        // Create disciplinary action
        app.MapPost("/api/v1/hr/disciplinary-actions", async (CreateDisciplinaryActionCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.DisciplinaryManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var action = await hr.CreateDisciplinaryActionAsync(new DisciplinaryAction(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                Enum.Parse<DisciplinaryActionType>(c.ActionType),
                Enum.Parse<DisciplinarySeverity>(c.Severity),
                c.Reason,
                c.Description,
                c.EffectiveDate,
                c.ExpiryDate), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.disciplinary.create:{action.Id}", 
                "disciplinaryaction", 
                action.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/disciplinary-actions/{action.Id}", action);
        });

        // Get disciplinary action by ID
        app.MapGet("/api/v1/hr/disciplinary-actions/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.DisciplinaryManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var action = await hr.GetDisciplinaryActionAsync(id, ct);
            if (action is null) return Results.NotFound();
            
            return Results.Ok(action);
        });

        // Get disciplinary actions by employee
        app.MapGet("/api/v1/hr/employees/{employeeId}/disciplinary-actions", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.DisciplinaryManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var actions = await hr.GetDisciplinaryActionsByEmployeeAsync(employeeId, ct);
            return Results.Ok(actions);
        });

        // Resolve disciplinary action
        app.MapPost("/api/v1/hr/disciplinary-actions/{id}/resolve", async (string id, ResolveDisciplinaryActionCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.DisciplinaryManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var action = await hr.ResolveDisciplinaryActionAsync(id, current.UserId, c.ResolutionNotes, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.disciplinary.resolve:{action.Id}", 
                "disciplinaryaction", 
                action.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(action);
        });

        // Expunge disciplinary action
        app.MapPost("/api/v1/hr/disciplinary-actions/{id}/expunge", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.DisciplinaryManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var action = await hr.ExpungeDisciplinaryActionAsync(id, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.disciplinary.expunge:{action.Id}", 
                "disciplinaryaction", 
                action.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(action);
        });

        // Cancel disciplinary action
        app.MapPost("/api/v1/hr/disciplinary-actions/{id}/cancel", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.DisciplinaryManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var action = await hr.CancelDisciplinaryActionAsync(id, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.disciplinary.cancel:{action.Id}", 
                "disciplinaryaction", 
                action.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(action);
        });
    }
}
