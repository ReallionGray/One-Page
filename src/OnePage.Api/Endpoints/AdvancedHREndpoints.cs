using OnePage.Platform;
using OnePage.Hr;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class AdvancedHREndpoints
{
    public static void MapAdvancedHREndpoints(this WebApplication app)
    {
        // Get current employment for employee
        app.MapGet("/api/v1/hr/employees/{employeeId}/employment", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employment = await hr.GetCurrentEmploymentAsync(employeeId, ct);
            if (employment is null) return Results.NotFound();
            
            return Results.Ok(employment);
        });

        // Get employment history for employee
        app.MapGet("/api/v1/hr/employees/{employeeId}/employments", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var employments = await hr.GetEmploymentsAsync(employeeId, ct);
            return Results.Ok(employments);
        });

        // Create leave policy
        app.MapPost("/api/v1/hr/leave-policies", async (CreateLeavePolicyCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var policy = new OnePage.Hr.LeavePolicy(c.Id, current.TenantId, c.Code, c.Name, c.AnnualEntitlement, c.AllowCarryover);
            await hr.AddAsync(policy, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.leave.policy.create:{policy.Id}", 
                "leavepolicy", 
                policy.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/leave-policies/{policy.Id}", policy);
        });

        // Create leave balance
        app.MapPost("/api/v1/hr/leave-balances", async (CreateLeaveBalanceCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var balance = new OnePage.Hr.LeaveBalance(c.Id, current.TenantId, c.EmployeeId, c.PolicyId, c.Year, c.EntitledDays);
            await hr.AddAsync(balance, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.leave.balance.create:{balance.Id}", 
                "leavebalance", 
                balance.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/leave-balances/{balance.Id}", balance);
        });

        // Get leave balance
        app.MapGet("/api/v1/hr/leave-balances/{employeeId}/{policyId}/{year}", async (string employeeId, string policyId, int year, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var balance = await hr.GetBalanceAsync(employeeId, policyId, year, ct);
            if (balance is null) return Results.NotFound();
            
            return Results.Ok(balance);
        });

        // Create leave request
        app.MapPost("/api/v1/hr/leave-requests", async (CreateLeaveRequestCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.LeaveRequest,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var request = new OnePage.Hr.LeaveRequest(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.PolicyId,
                c.StartDate,
                c.EndDate,
                c.Days,
                c.Reason);
            
            var createdRequest = await hr.CreateLeaveRequestAsync(request, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.leave.request.create:{createdRequest.Id}", 
                "leaverequest", 
                createdRequest.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/leave-requests/{createdRequest.Id}", createdRequest);
        });

        // Decide leave request
        app.MapPost("/api/v1/hr/leave-requests/{requestId}/decision", async (string requestId, DecideLeaveCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.LeaveApprove,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var decision = await hr.DecideLeaveAsync(requestId, current.UserId, c.Approve, c.Comment, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.leave.request.decide:{requestId}", 
                "leavedecision", 
                decision.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(decision);
        });

        // Get leave request with decisions
        app.MapGet("/api/v1/hr/leave-requests/{requestId}", async (string requestId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var request = await hr.GetLeaveRequestAsync(requestId, ct);
            if (request is null) return Results.NotFound();
            
            var decisions = await hr.GetLeaveDecisionsAsync(requestId, ct);
            
            return Results.Ok(new { request, decisions });
        });

        // Create onboarding/offboarding checklist item
        app.MapPost("/api/v1/hr/checklist-items", async (CreateChecklistItemCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var item = new OnePage.Hr.HrChecklistItem(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                Enum.Parse<ChecklistKind>(c.Kind),
                c.Title,
                c.OwnerUserId,
                c.DueDate);
            
            await hr.AddAsync(item, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.checklist.create:{item.Id}", 
                "checklistitem", 
                item.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/checklist-items/{item.Id}", item);
        });

        // Complete checklist item
        app.MapPost("/api/v1/hr/checklist-items/{itemId}/complete", async (string itemId, CompleteCheckItemCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var item = await hr.CompleteChecklistItemAsync(itemId, c.Evidence, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.checklist.complete:{item.Id}", 
                "checklistitem", 
                item.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(item);
        });

        // Create employee document
        app.MapPost("/api/v1/hr/employee-documents", async (CreateEmployeeDocumentCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IEmployeeDocumentStorage storage, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var fileReference = await storage.RegisterAsync(current.TenantId, c.FileReference, ct);
            
            var document = new OnePage.Hr.EmployeeDocument(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.DocumentType,
                fileReference,
                c.ExpiresOn);
            
            await hr.AddAsync(document, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.document.create:{document.Id}", 
                "employeedocument", 
                document.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/employee-documents/{document.Id}", document);
        });

        // Get employee document
        app.MapGet("/api/v1/hr/employee-documents/{documentId}", async (string documentId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var document = await hr.GetDocumentAsync(documentId, ct);
            if (document is null) return Results.NotFound();
            
            return Results.Ok(document);
        });

        // Import attendance data
        app.MapPost("/api/v1/hr/attendance/import", async (IAttendanceImportValidator validator, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.AttendanceManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            // This is a placeholder - in a real implementation, we'd parse the uploaded file
            // and validate it using the validator
            var result = new OnePage.Hr.AttendanceImportResult(new List<OnePage.Hr.AttendanceImportRow>(), new List<OnePage.Hr.AttendanceImportError>());
            
            return Results.Ok(result);
        });

        // Time and Attendance Endpoints (Advanced features)
        app.MapPost("/api/v1/hr/time/entries", async (CreateTimeEntryCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.AttendanceManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var entry = new OnePage.Hr.TimeEntry(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.ClockIn,
                c.ClockOut,
                c.Location,
                c.Notes);
            
            var createdEntry = await hr.CreateTimeEntryAsync(entry, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.time.entry.create:{createdEntry.Id}", 
                "timeentry", 
                createdEntry.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/time/entries/{createdEntry.Id}", createdEntry);
        });

        app.MapPost("/api/v1/hr/time/entries/{entryId}/clockout", async (string entryId, ClockOutCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.AttendanceManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var entry = await hr.ClockOutTimeEntryAsync(entryId, c.ClockOutTime, c.Notes, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.time.entry.clockout:{entryId}", 
                "timeentry", 
                entryId, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(entry);
        });

        app.MapGet("/api/v1/hr/time/entries/employee/{employeeId}", async (string employeeId, DateOnly? startDate, DateOnly? endDate, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.AttendanceManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var entries = await hr.GetTimeEntriesByEmployeeAsync(employeeId, startDate, endDate, ct);
            return Results.Ok(entries);
        });

        app.MapPost("/api/v1/hr/overtime/requests", async (CreateOvertimeRequestCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.AttendanceManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var request = new OnePage.Hr.OvertimeRequest(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.StartTime,
                c.EndTime,
                c.Hours,
                c.Reason,
                c.Description);
            
            var createdRequest = await hr.CreateOvertimeRequestAsync(request, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.overtime.request.create:{createdRequest.Id}", 
                "overtimerequest", 
                createdRequest.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/overtime/requests/{createdRequest.Id}", createdRequest);
        });

        app.MapPost("/api/v1/hr/overtime/requests/{requestId}/approve", async (string requestId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.AttendanceManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var request = await hr.ApproveOvertimeRequestAsync(requestId, current.UserId, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.overtime.request.approve:{requestId}", 
                "overtimerequest", 
                requestId, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(request);
        });

        // HR-specific Payroll Enhancement Endpoints
        app.MapPost("/api/v1/hr/payroll/records", async (CreateHrPayrollRecordCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = new OnePage.Hr.HrPayrollRecord(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.PayPeriodStart,
                c.PayPeriodEnd,
                c.GrossPay,
                c.TaxDeduction,
                c.PensionDeduction,
                c.NetPay,
                c.Currency);
            
            var createdRecord = await hr.CreatePayrollRecordAsync(record, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.payroll.record.create:{createdRecord.Id}", 
                "payrollrecord", 
                createdRecord.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/payroll/records/{createdRecord.Id}", createdRecord);
        });

        app.MapGet("/api/v1/hr/payroll/records/employee/{employeeId}", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var records = await hr.GetPayrollRecordsByEmployeeAsync(employeeId, ct);
            return Results.Ok(records);
        });

        app.MapPost("/api/v1/hr/payroll/records/{recordId}/process", async (string recordId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var record = await hr.ProcessPayrollRecordAsync(recordId, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.payroll.record.process:{recordId}", 
                "payrollrecord", 
                recordId, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(record);
        });

        app.MapPost("/api/v1/hr/loans", async (CreateEmployeeLoanCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var loan = new OnePage.Hr.EmployeeLoan(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.Amount,
                c.InterestRate,
                c.StartDate,
                c.EndDate,
                c.Description);
            
            var createdLoan = await hr.CreateEmployeeLoanAsync(loan, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.loan.create:{createdLoan.Id}", 
                "employeeloan", 
                createdLoan.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/loans/{createdLoan.Id}", createdLoan);
        });

        app.MapGet("/api/v1/hr/loans/employee/{employeeId}", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var loans = await hr.GetEmployeeLoansAsync(employeeId, ct);
            return Results.Ok(loans);
        });

        app.MapPost("/api/v1/hr/loans/{loanId}/repay", async (string loanId, RepayEmployeeLoanCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var loan = await hr.RepayEmployeeLoanAsync(loanId, c.Amount, ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.loan.repay:{loanId}", 
                "employeeloan", 
                loanId, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Ok(loan);
        });
    }
}
