using OnePage.Platform;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class PayrollEndpoints
{
    public static void MapPayrollEndpoints(this WebApplication app)
    {
        // Create payroll record
        app.MapPost("/api/v1/payroll/records", async (CreatePayrollRecordCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IHRRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            // Check both subscription and permission in one call
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            // Verify employee exists
            var employee = await hr.GetAsync(c.EmployeeId, ct);
            if (employee is null)
            {
                return Results.Problem(statusCode: 404, title: "Not found", detail: "Employee not found.");
            }
            
            var payrollRecord = await payroll.CreateAsync(new PayrollRecord(
                c.Id, 
                current.TenantId, 
                c.EmployeeId, 
                c.Amount, 
                c.Currency, 
                c.PeriodStart, 
                c.PeriodEnd, 
                c.Description), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.create:{payrollRecord.Id}", 
                "payroll", 
                payrollRecord.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/payroll/records/{payrollRecord.Id}", payrollRecord);
        });

        // Get payroll record by ID
        app.MapGet("/api/v1/payroll/records/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            return Results.Ok(record);
        });

        // List all payroll records
        app.MapGet("/api/v1/payroll/records", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var records = await payroll.ListAsync(current.TenantId, ct);
            return Results.Ok(records);
        });

        // List payroll records by employee
        app.MapGet("/api/v1/payroll/records/employee/{employeeId}", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var records = await payroll.ListByEmployeeAsync(current.TenantId, employeeId, ct);
            return Results.Ok(records);
        });

        // Process payroll record
        app.MapPost("/api/v1/payroll/records/{id}/process", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.Process(current.UserId);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.process:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Process payroll record with calculations
        app.MapPost("/api/v1/payroll/records/{id}/process-with-calculations", async (string id, ProcessPayrollWithCalculationsCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.ProcessWithCalculations(current.UserId, c.TaxRate, c.PensionRate);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.process.calculations:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Add bonus to payroll record
        app.MapPost("/api/v1/payroll/records/{id}/bonus", async (string id, AddBonusCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.AddBonus(c.BonusAmount);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.bonus:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Add deduction to payroll record
        app.MapPost("/api/v1/payroll/records/{id}/deduction", async (string id, AddDeductionCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.AddDeduction(c.DeductionAmount);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.deduction:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Set tax details for payroll record
        app.MapPost("/api/v1/payroll/records/{id}/tax", async (string id, SetTaxDetailsCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.SetTaxDetails(c.TaxCode, c.TaxRate);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.tax:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Set pension details for payroll record
        app.MapPost("/api/v1/payroll/records/{id}/pension", async (string id, SetPensionDetailsCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.SetPensionDetails(c.PensionScheme, c.PensionRate);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.pension:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Generate payslip for payroll record
        app.MapPost("/api/v1/payroll/records/{id}/payslip", async (string id, GeneratePayslipCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.GeneratePayslip(c.PayslipUrl);
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.payslip:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Pay payroll record
        app.MapPost("/api/v1/payroll/records/{id}/pay", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await payroll.GetAsync(id, ct);
            if (record is null) return Results.NotFound();
            
            record.Pay();
            await payroll.UpdateAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.record.pay:{record.Id}", 
                "payroll", 
                record.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        // Get total payroll for a period
        app.MapGet("/api/v1/payroll/total", async (DateTimeOffset periodStart, DateTimeOffset periodEnd, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var total = await payroll.GetTotalPayrollAsync(current.TenantId, periodStart, periodEnd, ct);
            return Results.Ok(new { total });
        });

        // Run payroll for all employees
        app.MapPost("/api/v1/payroll/run", async (RunPayrollCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IPayrollRepository payroll, IHRRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Payroll, 
                PermissionCatalog.PayrollRun,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            // Get all active employees
            var employees = await hr.ListAsync(current.TenantId, ct);
            var activeEmployees = employees.Where(e => e.Status == "active").ToList();
            
            var createdRecords = new List<PayrollRecord>();
            foreach (var employee in activeEmployees)
            {
                var record = new PayrollRecord(
                    Guid.NewGuid().ToString("N"),
                    current.TenantId,
                    employee.Id,
                    employee.Salary,
                    c.Currency,
                    c.PeriodStart,
                    c.PeriodEnd,
                    $"Monthly payroll for {employee.FirstName} {employee.LastName}");
                
                await payroll.CreateAsync(record, ct);
                createdRecords.Add(record);
            }
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"payroll.run:{c.PeriodStart:yyyy-MM}:{c.PeriodEnd:yyyy-MM}", 
                "payroll", 
                null, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(new { 
                count = createdRecords.Count, 
                totalAmount = createdRecords.Sum(r => r.Amount),
                periodStart = c.PeriodStart,
                periodEnd = c.PeriodEnd,
                records = createdRecords.Select(r => new { r.Id, r.EmployeeId, r.Amount, r.Currency, r.Status }) });
        });
    }
}
