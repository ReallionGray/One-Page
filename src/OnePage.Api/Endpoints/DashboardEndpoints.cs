using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Api.Endpoints;

public static class DashboardEndpoints
{
    /// <summary>
    /// Returns a single aggregated payload of dashboard analytics, metrics, and
    /// schedule/calendar events for the current tenant. The SPA dashboard
    /// consumes this in one request so the charts and calendar can be rendered
    /// without round-tripping through tables.
    /// </summary>
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/dashboard", async (ITenantContextAccessor ctx, OrganizationDbContext db, IModuleAccessEvaluator moduleAccessEvaluator, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var tenantId = current.TenantId;

            // Determine accessible modules
            var moduleAccessTasks = new Dictionary<string, Task<ModuleAccessDecision>>
            {
                ["assets"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Assets, null, null, null, ct),
                ["hr"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Hr, null, null, null, ct),
                ["procurement"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Procurement, null, null, null, ct),
                ["inventory"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Inventory, null, null, null, ct),
                ["pos"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Pos, null, null, null, ct),
                ["finance"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Finance, null, null, null, ct),
                ["payroll"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Payroll, null, null, null, ct),
                ["reporting"] = moduleAccessEvaluator.EvaluateModuleAccessAsync(current, EntitlementKeys.Modules.Reporting, null, null, null, ct)
            };

            await Task.WhenAll(moduleAccessTasks.Values);
            var accessibleModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in moduleAccessTasks)
            {
                if (kvp.Value.Result.Allowed)
                {
                    accessibleModules.Add(kvp.Key);
                }
            }

            // Load the cross-module data needed for aggregate analytics + calendar.
            // Demo datasets are small; materializing them lets us aggregate reliably
            // across both SQLite and PostgreSQL providers without provider-specific
            // date-part translation.
            var assets = accessibleModules.Contains("assets") ? await db.Assets.AsNoTracking().Where(a => a.TenantId == tenantId).ToListAsync(ct) : new List<Asset>();
            var employees = accessibleModules.Contains("hr") ? await db.Employees.AsNoTracking().Where(e => e.TenantId == tenantId).ToListAsync(ct) : new List<Employee>();
            // Approvals are produced by several modules (assets disposals, inventory
            // adjustments, purchase orders), so surface them whenever the user can open
            // any of those — not only when the assets module is accessible.
            var approvals = (accessibleModules.Contains("assets") || accessibleModules.Contains("procurement") || accessibleModules.Contains("inventory")) ? await db.ApprovalRequests.AsNoTracking().Where(a => a.TenantId == tenantId).ToListAsync(ct) : new List<ApprovalRequest>();
            var inventory = accessibleModules.Contains("inventory") ? await db.InventoryItems.AsNoTracking().Where(i => i.TenantId == tenantId).ToListAsync(ct) : new List<InventoryItem>();
            var sales = accessibleModules.Contains("pos") ? await db.PosSales.AsNoTracking().Where(s => s.TenantId == tenantId).ToListAsync(ct) : new List<PosSale>();
            var purchaseOrders = accessibleModules.Contains("procurement") ? await db.PurchaseOrders.AsNoTracking().Where(p => p.TenantId == tenantId).ToListAsync(ct) : new List<PurchaseOrder>();
            var journalEntries = accessibleModules.Contains("finance") ? await db.JournalEntries.AsNoTracking().Where(j => j.TenantId == tenantId).ToListAsync(ct) : new List<JournalEntry>();
            var payroll = accessibleModules.Contains("payroll") ? await db.PayrollRecords.AsNoTracking().Where(p => p.TenantId == tenantId).ToListAsync(ct) : new List<PayrollRecord>();
            var departments = accessibleModules.Contains("hr") ? await db.Departments.AsNoTracking().Where(d => d.TenantId == tenantId).ToListAsync(ct) : new List<Department>();
            var audit = await db.AuditEvents.AsNoTracking().Where(a => a.TenantId == tenantId).ToListAsync(ct); // Always load audit events
            // Mechanical "API call" entries are recorded by the request audit middleware
            // (action = "{HTTP_METHOD} {path}", e.g. "GET /api/v1/assets"). They are emitted
            // for every request and are not meaningful user activities, so exclude them
            // from the dashboard analytics and activity feed. Semantic business events
            // (action = "module.action:entityId") are always preserved.
            var meaningfulAudit = audit.Where(a => !IsMechanicalApiCall(a.Action)).ToList();

            var deptMap = departments.ToDictionary(d => d.Id, d => d.Name);
            var tenantName = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.Name).FirstOrDefaultAsync(ct);
            string DeptName(string? id) => string.IsNullOrWhiteSpace(id)
                ? "Unassigned"
                : (deptMap.TryGetValue(id!, out var n) ? n : id!);

            // ---- Metrics (summary cards) ----
            var metrics = new
            {
                totalAssets = assets.Count,
                activeEmployees = employees.Count(e => e.Status == "active"),
                pendingApprovals = approvals.Count(a => a.Status == "pending"),
                totalSales = Math.Round(sales.Sum(s => s.Total), 2),
                inventoryItemCount = inventory.Count,
                totalInventoryQuantity = Math.Round(inventory.Sum(i => i.Quantity), 2),
                totalPurchaseOrders = purchaseOrders.Count,
                totalJournalEntries = journalEntries.Count,
                totalPayroll = Math.Round(payroll.Where(r => r.Status == "paid").Sum(r => r.Amount), 2)
            };

            // ---- Analytics (charts / graphs) ----
            var assetStatus = assets.Any() ? assets
                .GroupBy(a => a.Status)
                .Select(g => new DashboardChartSegment(g.Key, g.Count()))
                .OrderBy(x => x.Label)
                .ToList() : new List<DashboardChartSegment>();

            var inventoryBySku = inventory.Any() ? inventory
                .OrderByDescending(i => i.Quantity)
                .Select(i => (object)new { i.Sku, i.Name, quantity = Math.Round(i.Quantity, 2) })
                .ToList() : new List<object>();

            var salesTrend = sales.Any() ? sales
                .GroupBy(s => s.CreatedAt.ToString("yyyy-MM"))
                .Select(g => new { period = g.Key, total = Math.Round(g.Sum(x => x.Total), 2) })
                .OrderBy(x => x.period)
                .Select(x => (object)x)
                .ToList() : new List<object>();

            var employeeDept = employees.ToDictionary(e => e.Id, e => e.DepartmentId);
            var payrollByDepartment = payroll.Any() ? payroll
                .GroupBy(p => employeeDept.TryGetValue(p.EmployeeId, out var d) ? d : null)
                .Select(g => new DashboardChartSegment(DeptName(g.Key), Math.Round(g.Sum(p => p.Amount), 2)))
                .OrderBy(x => x.Label)
                .ToList() : new List<DashboardChartSegment>();

            var employeeByDepartment = employees.Any() ? employees
                .GroupBy(e => e.DepartmentId)
                .Select(g => new DashboardChartSegment(DeptName(g.Key), g.Count()))
                .OrderBy(x => x.Label)
                .ToList() : new List<DashboardChartSegment>();

            var purchaseOrderByStatus = purchaseOrders.Any() ? purchaseOrders
                .GroupBy(p => p.Status)
                .Select(g => new DashboardChartSegment(g.Key, g.Count()))
                .OrderBy(x => x.Label)
                .ToList() : new List<DashboardChartSegment>();

            var employeeStatus = employees.Any() ? employees
                .GroupBy(e => e.Status)
                .Select(g => new DashboardChartSegment(g.Key, g.Count()))
                .OrderBy(x => x.Label)
                .ToList() : new List<DashboardChartSegment>();

            // Audit activity by day (last 30 distinct days with events). Only meaningful
            // (semantic) events are counted so the timeline reflects actual user activity
            // rather than every API request being served.
            var auditActivity = meaningfulAudit
                .GroupBy(a => a.CreatedAt.ToString("yyyy-MM-dd"))
                .Select(g => new { date = g.Key, count = g.Count() })
                .OrderBy(x => x.date)
                .TakeLast(30) // cap for the timeline chart
                .ToList();

            // Curated recent-activity feed projected for the client: the newest 100
            // meaningful events only (no mechanical API-call entries). The dashboard
            // renders this directly instead of calling the ReportExport-gated export.
            var activityEvents = meaningfulAudit
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .Select(a => new
                {
                    a.Id, a.Action, a.ResourceType, a.ResourceId, a.ActorUserId, a.CreatedAt
                })
                .ToList();

            var analytics = new
            {
                assetStatus,
                inventoryBySku,
                salesTrend,
                payrollByDepartment,
                employeeByDepartment,
                purchaseOrderByStatus,
                employeeStatus,
                auditActivity
            };

            // ---- Schedule / calendar events ----
            var now = DateTimeOffset.UtcNow;
            var schedule = new List<ScheduleEvent>();

            // Payroll run periods (use the period start as the scheduled date)
            foreach (var p in payroll)
            {
                var emp = employees.FirstOrDefault(e => e.Id == p.EmployeeId);
                var empName = emp == null ? p.EmployeeId : $"{emp.FirstName} {emp.LastName}";
                schedule.Add(new ScheduleEvent(
                    p.PeriodStart.ToString("yyyy-MM-dd"),
                    "payroll",
                    $"Payroll for {empName}",
                    $"Period {p.PeriodStart:yyyy-MM-dd} → {p.PeriodEnd:yyyy-MM-dd} · {p.Amount} {p.Currency} ({p.Status})",
                    p.Status));
            }

            // Pending approvals (due from creation date)
            foreach (var a in approvals.Where(a => a.Status == "pending"))
            {
                schedule.Add(new ScheduleEvent(
                    a.CreatedAt.ToString("yyyy-MM-dd"),
                    "approval",
                    $"Approval: {a.ResourceType}",
                    a.ResourceId ?? a.Reason ?? "Awaiting review",
                    a.Status));
            }

            // Work anniversaries for active employees (next occurrence this year or next)
            foreach (var e in employees.Where(e => e.Status == "active"))
            {
                var anniversary = new DateTimeOffset(now.Year, e.HireDate.Month, e.HireDate.Day, 0, 0, 0, TimeSpan.Zero);
                if (anniversary < now) anniversary = anniversary.AddYears(1);
                var years = anniversary.Year - e.HireDate.Year;
                schedule.Add(new ScheduleEvent(
                    anniversary.ToString("yyyy-MM-dd"),
                    "anniversary",
                    $"Anniversary: {e.FirstName} {e.LastName}",
                    $"{years} year(s) · {e.Position ?? "—"}",
                    e.Status));
            }

            // Recent purchase orders (use creation date)
            foreach (var po in purchaseOrders)
            {
                schedule.Add(new ScheduleEvent(
                    po.CreatedAt.ToString("yyyy-MM-dd"),
                    "purchase",
                    $"PO: {po.Supplier}",
                    $"{po.TotalAmount} · {po.Status}",
                    po.Status));
            }

            // De-duplicate by (date, type, title) and sort chronologically
            var seen = new HashSet<(string, string, string)>();
            var deduped = new List<ScheduleEvent>();
            foreach (var ev in schedule.OrderBy(s => s.Date))
            {
                var key = (ev.Date, ev.Type, ev.Title);
                if (!seen.Add(key)) continue;
                deduped.Add(ev);
            }

            return Results.Ok(new
            {
                generatedAt = now,
                tenantId,
                tenantName = tenantName,
                metrics,
                analytics,
                schedule = deduped,
                activityEvents
            });
        });
    }

    /// <summary>
    /// Returns true for mechanical audit entries recorded by the request audit
    /// middleware, whose <c>Action</c> is formatted as "<c>{HTTP_METHOD} {path}</c>"
    /// (e.g. "GET /api/v1/assets"). These are emitted for every request and are
    /// not meaningful user activities, so they are excluded from the dashboard's
    /// activity analytics and feed. Semantic business events use an
    /// "<c>module.action:entityId</c>" shape and are always preserved.
    /// </summary>
    private static bool IsMechanicalApiCall(string action)
    {
        if (string.IsNullOrWhiteSpace(action)) return false;
        foreach (var verb in new[] { "GET ", "POST ", "PUT ", "PATCH ", "DELETE ", "HEAD ", "OPTIONS " })
        {
            if (action.StartsWith(verb, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
