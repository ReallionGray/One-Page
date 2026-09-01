using OnePage.Platform;
using OnePage.Hr;
using HrEmployee = OnePage.Hr.Employee;
using HrEmployment = OnePage.Hr.Employment;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class HREndpoints
{
    /// <summary>
    /// Returns a redacted employee response that masks sensitive fields
    /// (Email, Phone, GovernmentId) with "[redacted]" unless they are null.
    /// </summary>
    private static EmployeeResponse ToRedactedResponse(HrEmployee e) => new(
        e.Id, e.TenantId, e.EmployeeNumber, e.FirstName, e.LastName,
        e.Email is not null ? "[redacted]" : null,
        e.Phone is not null ? "[redacted]" : null,
        e.GovernmentId is not null ? "[redacted]" : null,
        e.IsActive, e.TerminationDate, e.CreatedAt);

    public static void MapHREndpoints(this WebApplication app)
    {
        // Create advanced employee (with employee number and government ID)
        app.MapPost("/api/v1/hr/employees", async (CreateAdvancedEmployeeCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeCreate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employee = await hr.CreateEmployeeAsync(new HrEmployee(
                c.Id, 
                current.TenantId, 
                c.EmployeeNumber,
                c.FirstName, 
                c.LastName, 
                c.Email, 
                c.Phone,
                c.GovernmentId), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.employee.create:{employee.Id}", 
                "employee", 
                employee.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/employees/{employee.Id}", ToRedactedResponse(employee));
        });

        // Get employee by ID (sensitive fields are redacted)
        app.MapGet("/api/v1/hr/employees/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employee = await hr.GetEmployeeAsync(id, ct);
            if (employee is null) return Results.NotFound();
            
            return Results.Ok(ToRedactedResponse(employee));
        });

        // List all employees (sensitive fields are redacted)
        app.MapGet("/api/v1/hr/employees", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employees = await hr.ListEmployeesAsync(ct);
            return Results.Ok(employees.Select(ToRedactedResponse));
        });

        // Create employment record for a specific employee
        app.MapPost("/api/v1/hr/employees/{employeeId}/employment", async (string employeeId, CreateEmploymentCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employment = await hr.CreateEmploymentAsync(new HrEmployment(
                c.Id,
                current.TenantId,
                employeeId,
                c.EffectiveFrom,
                c.LegalEntityId,
                c.DepartmentId,
                c.Position,
                c.ManagerEmployeeId,
                c.LocationId,
                c.Status,
                c.EffectiveTo), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.employment.create:{employment.Id}", 
                "employment", 
                employment.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/employments/{employment.Id}", employment);
        });

        // Offboard employee
        app.MapPost("/api/v1/hr/employees/{employeeId}/offboard", async (string employeeId, OffboardEmployeeCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeTerminate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employee = await hr.OffboardEmployeeAsync(employeeId, c.EffectiveDate, current.UserId, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.employee.offboard:{employee.Id}", 
                "employee", 
                employee.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(ToRedactedResponse(employee));
        });
    }
}
