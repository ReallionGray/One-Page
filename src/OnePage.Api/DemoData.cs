using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Api;

internal static class DemoData
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var ent = scope.ServiceProvider.GetRequiredService<InMemoryEntitlementEvaluator>();

        // Seed demo tenant
        const string tenantId = "demo-tenant";
        var existing = await db.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
        if (existing is null)
        {
            db.Tenants.Add(new Tenant(tenantId, "Demo Tenant", SubscriptionPlan.Enterprise));
        }
        else
        {
            // Update existing tenant to have enterprise plan
            existing.UpgradeSubscription(SubscriptionPlan.Enterprise);
        }

        // Create different roles with appropriate permissions
        var roles = new Dictionary<string, PermissionKey[]>
        {
            // SuperAdmin has all access and permissions, including management of users, roles, and permissions
            ["SuperAdmin"] = new[] {
                PermissionCatalog.AssetCreate, PermissionCatalog.AssetView, PermissionCatalog.AssetAssign, PermissionCatalog.AssetDispose, PermissionCatalog.AssetTransfer,
                PermissionCatalog.PurchaseOrderCreate, PermissionCatalog.PurchaseOrderApprove, PermissionCatalog.InventoryAdjust, PermissionCatalog.InventoryView,
                PermissionCatalog.PosSaleCreate, PermissionCatalog.FinanceJournalExport, PermissionCatalog.ApprovalReview,
                PermissionCatalog.ReportRun, PermissionCatalog.ReportExport, PermissionCatalog.EmployeeView, PermissionCatalog.PayrollRun,
                PermissionCatalog.UserManage, PermissionCatalog.RoleManage, PermissionCatalog.PermissionManage, PermissionCatalog.OrganizationManage,
                PermissionCatalog.WorkflowManage
            },
            // Organization Admin can manage users and roles within their own organization
            ["admin"] = new[] {
                PermissionCatalog.AssetCreate, PermissionCatalog.AssetView, PermissionCatalog.AssetAssign, PermissionCatalog.AssetDispose, PermissionCatalog.ApprovalReview,
                PermissionCatalog.PurchaseOrderCreate, PermissionCatalog.PurchaseOrderApprove, PermissionCatalog.InventoryAdjust, PermissionCatalog.PosSaleCreate,
                PermissionCatalog.FinanceJournalExport, PermissionCatalog.ReportRun, PermissionCatalog.ReportExport,
                PermissionCatalog.UserManage, PermissionCatalog.RoleManage, PermissionCatalog.PermissionManage, PermissionCatalog.WorkflowManage
            },
            ["hrmanager"] = new[] {
                PermissionCatalog.EmployeeView, PermissionCatalog.PayrollRun, PermissionCatalog.ReportRun, PermissionCatalog.ReportExport
            },
            ["accountant"] = new[] {
                PermissionCatalog.FinanceJournalExport, PermissionCatalog.ReportRun, PermissionCatalog.ReportExport, PermissionCatalog.AssetView
            },
            ["sales"] = new[] {
                PermissionCatalog.PosSaleCreate, PermissionCatalog.InventoryView, PermissionCatalog.AssetView
            },
            ["user"] = new[] {
                PermissionCatalog.AssetView, PermissionCatalog.InventoryView
            }
        };

        // Create roles and assign permissions
        foreach (var roleEntry in roles)
        {
            var roleName = roleEntry.Key;
            var permissions = roleEntry.Value;
            var roleId = $"role-{roleName}";
            
            var role = await db.Roles.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Name == roleName, cancellationToken);
            if (role is null) db.Roles.Add(new Role(roleId, tenantId, roleName));

            // Ensure permissions exist for the role
            foreach (var p in permissions)
            {
                var rpId = $"rp-{roleId}-{p.ToString().Replace('.', '-')}";
                if (!await db.RolePermissions.AnyAsync(rp => rp.TenantId == tenantId && rp.RoleId == roleId && rp.Permission == p.ToString(), cancellationToken))
                {
                    db.RolePermissions.Add(new RolePermission(rpId, tenantId, roleId, p));
                }
            }
        }

        // Create user memberships and assign roles
        var userRoleAssignments = new Dictionary<string, string>
        {
            ["emp-001"] = "SuperAdmin", // This will be handled specially in authentication
            ["emp-002"] = "admin", 
            ["emp-003"] = "hrmanager",
            ["emp-004"] = "accountant",
            ["emp-005"] = "sales",
            ["emp-006"] = "user"
        };
        
        // Special handling for superadmin - also create the super-admin user ID mapping
        var superAdminMembership = await db.UserMemberships.SingleOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == "super-admin", cancellationToken);
        if (superAdminMembership is null)
        {
            db.UserMemberships.Add(new UserMembership("membership-super-admin", tenantId, "super-admin"));
        }
        
        // Link super-admin to SuperAdmin role
        var superAdminRoleId = "role-SuperAdmin";
        if (!await db.MembershipRoleAssignments.AnyAsync(a => a.TenantId == tenantId && a.MembershipId == "membership-super-admin" && a.RoleId == superAdminRoleId, cancellationToken))
        {
            db.MembershipRoleAssignments.Add(new MembershipRoleAssignment(Guid.NewGuid().ToString("N"), tenantId, "membership-super-admin", superAdminRoleId));
        }

        foreach (var userAssignment in userRoleAssignments)
        {
            var employeeId = userAssignment.Key;
            var roleName = userAssignment.Value;
            var membershipId = $"membership-{employeeId}";
            var roleId = $"role-{roleName}";
            
            // Create membership
            var membership = await db.UserMemberships.SingleOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == employeeId, cancellationToken);
            if (membership is null)
            {
                db.UserMemberships.Add(new UserMembership(membershipId, tenantId, employeeId));
            }

            // Link membership to role
            if (!await db.MembershipRoleAssignments.AnyAsync(a => a.TenantId == tenantId && a.MembershipId == membershipId && a.RoleId == roleId, cancellationToken))
            {
                db.MembershipRoleAssignments.Add(new MembershipRoleAssignment(Guid.NewGuid().ToString("N"), tenantId, membershipId, roleId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Seed some demo inventory SKUs
        var skuList = new[] {
            new InventoryItem("inv-sku-001", tenantId, "SKU-001", "Demo Widget", 100),
            new InventoryItem("inv-sku-002", tenantId, "SKU-002", "Demo Gizmo", 50),
            new InventoryItem("inv-sku-003", tenantId, "SKU-003", "Demo Thingamajig", 10)
        };
        foreach (var sku in skuList)
        {
            if (!await db.InventoryItems.AnyAsync(i => i.TenantId == tenantId && i.Sku == sku.Sku, cancellationToken))
            {
                db.InventoryItems.Add(sku);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Seed demo assets
        var assetList = new[] {
            new Asset("asset-demo-001", tenantId, "ASSET-1001", "Conference Room Projector", "Epson projector", "HQ", null),
            new Asset("asset-demo-002", tenantId, "ASSET-1002", "MacBook Pro 14", "Engineering laptop", "HQ", "employee-1")
        };
        foreach (var asset in assetList)
        {
            if (!await db.Assets.AnyAsync(a => a.TenantId == tenantId && a.Id == asset.Id, cancellationToken))
                db.Assets.Add(asset);
        }

        // Seed a demo purchase order
        if (!await db.PurchaseOrders.AnyAsync(p => p.TenantId == tenantId && p.Id == "po-demo-001", cancellationToken))
            db.PurchaseOrders.Add(new PurchaseOrder("po-demo-001", tenantId, "Acme Supplies Ltd", 1250.00m));

        // Seed a pending approval from another user so demo-user can approve it
        var approval1 = new ApprovalRequest("approval-demo-001", tenantId, "purchase_order.approve", "po-demo-001", "emp-006", "1250.00");
        approval1.AttachWorkflow("wf-po-standard");
        if (!await db.ApprovalRequests.AnyAsync(a => a.TenantId == tenantId && a.Id == "approval-demo-001", cancellationToken))
            db.ApprovalRequests.Add(approval1);

        // Seed a second pending approval requested by a different user — this one
        // is at step 2 (admin needs to approve first, then hrmanager).
        var approval2 = new ApprovalRequest("approval-demo-002", tenantId, "purchase_order.approve", "po-demo-001", "emp-005", "3200.00");
        approval2.AttachWorkflow("wf-po-standard");
        if (!await db.ApprovalRequests.AnyAsync(a => a.TenantId == tenantId && a.Id == "approval-demo-002", cancellationToken))
            db.ApprovalRequests.Add(approval2);

        // Seed a completed approval (approved) — requested by the demo super-admin
        // so it shows up in the Completed tab for the logged-in super-admin user.
        if (!await db.ApprovalRequests.AnyAsync(a => a.TenantId == tenantId && a.Id == "approval-demo-003", cancellationToken))
        {
            var completed = new ApprovalRequest("approval-demo-003", tenantId, "purchase_order.approve", "po-demo-002", "super-admin", "750.00");
            completed.AttachWorkflow("wf-po-simple");
            completed.Approve("emp-002", "Approved per amount threshold workflow");
            db.ApprovalRequests.Add(completed);
        }

        // Seed the per-step decision for the completed approval
        if (!await db.ApprovalDecisions.AnyAsync(a => a.TenantId == tenantId && a.ApprovalRequestId == "approval-demo-003", cancellationToken))
        {
            db.ApprovalDecisions.Add(new ApprovalDecision("decision-demo-003-step1", tenantId, "approval-demo-003", 1, "emp-002", "approved", "Approved per amount threshold workflow"));
        }

        // Seed a third pending approval for asset disposal using the always-trigger workflow
        var approval4 = new ApprovalRequest("approval-demo-004", tenantId, "asset.dispose", "asset-demo-001", "emp-005", "Dispose old projector");
        approval4.AttachWorkflow("wf-asset-dispose");
        if (!await db.ApprovalRequests.AnyAsync(a => a.TenantId == tenantId && a.Id == "approval-demo-004", cancellationToken))
            db.ApprovalRequests.Add(approval4);

        // Seed workflow definitions per organization
        if (!await db.WorkflowDefinitions.AnyAsync(w => w.TenantId == tenantId && w.Id == "wf-po-standard", cancellationToken))
        {
            db.WorkflowDefinitions.Add(new WorkflowDefinition(
                "wf-po-standard", tenantId, "Standard PO Approval", "purchase_order.approve",
                "amount", 1000m, true, "Two-step approval: admin first, then HR manager"));
            db.WorkflowSteps.Add(new WorkflowStep("wf-step-1", tenantId, "wf-po-standard", 1, "role", "admin"));
            db.WorkflowSteps.Add(new WorkflowStep("wf-step-2", tenantId, "wf-po-standard", 2, "role", "hrmanager"));
        }

        if (!await db.WorkflowDefinitions.AnyAsync(w => w.TenantId == tenantId && w.Id == "wf-po-simple", cancellationToken))
        {
            db.WorkflowDefinitions.Add(new WorkflowDefinition(
                "wf-po-simple", tenantId, "Simple PO Approval", "purchase_order.approve",
                "always", null, true, "Single approver for all purchase orders"));
            db.WorkflowSteps.Add(new WorkflowStep("wf-simple-step-1", tenantId, "wf-po-simple", 1, "role", "admin"));
        }

        // Asset disposal workflow — always triggers, single admin approver
        if (!await db.WorkflowDefinitions.AnyAsync(w => w.TenantId == tenantId && w.Id == "wf-asset-dispose", cancellationToken))
        {
            db.WorkflowDefinitions.Add(new WorkflowDefinition(
                "wf-asset-dispose", tenantId, "Asset Disposal Approval", "asset.dispose",
                "always", null, true, "Asset disposal requires admin approval"));
            db.WorkflowSteps.Add(new WorkflowStep("wf-asset-step-1", tenantId, "wf-asset-dispose", 1, "role", "admin"));
        }

        // Inventory adjustment workflow — triggers on large adjustments
        if (!await db.WorkflowDefinitions.AnyAsync(w => w.TenantId == tenantId && w.Id == "wf-inventory-adjust", cancellationToken))
        {
            db.WorkflowDefinitions.Add(new WorkflowDefinition(
                "wf-inventory-adjust", tenantId, "Inventory Adjustment Approval", "inventory.adjust",
                "amount", 500m, true, "Large inventory adjustments require admin approval"));
            db.WorkflowSteps.Add(new WorkflowStep("wf-inv-step-1", tenantId, "wf-inventory-adjust", 1, "role", "admin"));
        }

        // Seed demo departments
        var deptList = new[] {
            new Department("dept-eng", tenantId, "Engineering"),
            new Department("dept-sales", tenantId, "Sales"),
            new Department("dept-hr", tenantId, "Human Resources"),
            new Department("dept-finance", tenantId, "Finance")
        };
        foreach (var dept in deptList)
        {
            if (!await db.Departments.AnyAsync(d => d.TenantId == tenantId && d.Id == dept.Id, cancellationToken))
                db.Departments.Add(dept);
        }

        // Seed demo employees with emails matching login credentials
        var employeeList = new[] {
            new Employee("emp-001", tenantId, "Super", "Admin", "superadmin@demo.com", "dept-eng", "Super Administrator", 100000),
            new Employee("emp-002", tenantId, "Admin", "User", "admin@demo.com", "dept-eng", "Administrator", 85000),
            new Employee("emp-003", tenantId, "HR", "Manager", "hrmanager@demo.com", "dept-hr", "HR Manager", 75000),
            new Employee("emp-004", tenantId, "Accountant", "User", "accountant@demo.com", "dept-finance", "Accountant", 70000),
            new Employee("emp-005", tenantId, "Sales", "User", "sales@demo.com", "dept-sales", "Sales Representative", 55000),
            new Employee("emp-006", tenantId, "Regular", "User", "user@demo.com", "dept-eng", "Developer", 65000)
        };
        foreach (var emp in employeeList)
        {
            if (!await db.Employees.AnyAsync(e => e.TenantId == tenantId && e.Id == emp.Id, cancellationToken))
                db.Employees.Add(emp);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Seed user profiles (used by profile/settings endpoints)
        // Note: super-admin login maps to userId "super-admin", not emp-001
        var profileList = new[]
        {
            new UserProfile("super-admin", "Super", "Admin", "superadmin@demo.com"),
            new UserProfile("emp-002", "Admin", "User", "admin@demo.com"),
            new UserProfile("emp-003", "HR", "Manager", "hrmanager@demo.com"),
            new UserProfile("emp-004", "Accountant", "User", "accountant@demo.com"),
            new UserProfile("emp-005", "Sales", "User", "sales@demo.com"),
            new UserProfile("emp-006", "Regular", "User", "user@demo.com")
        };
        foreach (var profile in profileList)
        {
            if (!await db.UserProfiles.AnyAsync(p => p.UserId == profile.UserId, cancellationToken))
                db.UserProfiles.Add(profile);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Seed recent, semantically-meaningful audit events (never mechanical API-call
        // entries) so the dashboard's activity charts and "Recent Activity" feed are
        // populated on first sight and demonstrate that the feed surfaces real business
        // events rather than raw request logs. CreatedAt has a private setter, so it is
        // back-dated via reflection to spread events across the last few days.
        // Idempotent on re-seed.
        var demoNow = DateTimeOffset.UtcNow;
        var demoCreatedAt = typeof(AuditEvent).GetProperty("CreatedAt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var demoAudit = new[]
        {
            ("hr.employee.create",        "hr",        "emp-002"),
            ("hr.payroll.record.process", "hr",        "payroll-run-001"),
            ("pos.sale.create",           "pos",       "sale-1001"),
            ("finance.journal.create",    "finance",   "je-2024-10"),
            ("inventory.adjust",          "inventory", "sku-WIDGET"),
            ("asset.create",              "asset",     "asset-demo-009"),
            ("hr.leave.request.create",   "hr",        "LR-1001"),
            ("hr.time.entry.create",      "hr",        "time-emp-005"),
            ("approval.step.approve",     "approval",  "approval-demo-003")
        };
        int demoOffset = 0;
        foreach (var (action, resourceType, resourceId) in demoAudit)
        {
            var actionKey = action + ":" + resourceId;
            if (await db.AuditEvents.AnyAsync(a => a.TenantId == tenantId && a.Action == actionKey, cancellationToken)) continue;
            var actor = "emp-00" + ((demoOffset % 5) + 2); // emp-002..emp-006 (seeded employees)
            var evt = new AuditEvent(Guid.NewGuid().ToString("N"), tenantId, actor, actionKey, resourceType, resourceId, null, null, "demo-seed", null, null);
            demoCreatedAt.SetValue(evt, demoNow.AddDays(-(demoOffset % 7)).AddHours(-(demoOffset * 3)));
            db.AuditEvents.Add(evt);
            demoOffset++;
        }
        await db.SaveChangesAsync(cancellationToken);

        // Second organization so admin@demo.com belongs to more than one tenant and
        // the login org-picker can be exercised.
        await SeedSecondaryTenantAsync(db, cancellationToken);

        // Add entitlement assignments for enterprise plan
        var enterpriseModules = new[]
        {
            EntitlementKeys.Modules.Assets,
            EntitlementKeys.Modules.Procurement,
            EntitlementKeys.Modules.Inventory,
            EntitlementKeys.Modules.Pos,
            EntitlementKeys.Modules.Finance,
            EntitlementKeys.Modules.Reporting,
            EntitlementKeys.Modules.Hr,
            EntitlementKeys.Modules.Payroll,
            EntitlementKeys.Features.AdvancedReporting
        };

        foreach (var moduleKey in enterpriseModules)
        {
            var assignmentId = $"ent-{tenantId}-{moduleKey.Namespace}-{moduleKey.Name}";
            if (!await db.EntitlementAssignments.AnyAsync(e => e.TenantId == tenantId && e.EntitlementNamespace == moduleKey.Namespace && e.EntitlementName == moduleKey.Name, cancellationToken))
            {
                db.EntitlementAssignments.Add(new EntitlementAssignment(
                    assignmentId, 
                    tenantId, 
                    moduleKey.Namespace, 
                    moduleKey.Name, 
                    EntitlementState.Active, 
                    "enterprise-plan"));
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Set entitlements active for demo tenant (legacy support for in-memory evaluator)
        var defActive = new EntitlementDefinition(EntitlementKeys.Modules.Assets, EntitlementState.Active, "demo-seed");
        ent.Set(tenantId, defActive);
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Procurement, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Inventory, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Pos, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Finance, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Reporting, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Hr, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Modules.Payroll, EntitlementState.Active, "demo-seed"));
        ent.Set(tenantId, new EntitlementDefinition(EntitlementKeys.Features.AdvancedReporting, EntitlementState.Active, "demo-seed"));
    }

    private static async Task SeedSecondaryTenantAsync(OrganizationDbContext db, CancellationToken cancellationToken)
    {
        const string tenantId = "acme-tenant";
        if (await db.Tenants.FindAsync(new object[] { tenantId }, cancellationToken) is null)
            db.Tenants.Add(new Tenant(tenantId, "Acme Corporation", SubscriptionPlan.Professional));

        const string roleId = "acme-role-admin";
        if (!await db.Roles.AnyAsync(r => r.TenantId == tenantId && r.Id == roleId, cancellationToken))
            db.Roles.Add(new Role(roleId, tenantId, "admin"));

        var adminPermissions = new[]
        {
            PermissionCatalog.AssetCreate, PermissionCatalog.AssetView, PermissionCatalog.AssetAssign,
            PermissionCatalog.PurchaseOrderCreate, PermissionCatalog.InventoryView, PermissionCatalog.PosSaleCreate,
            PermissionCatalog.ReportRun, PermissionCatalog.EmployeeView,
            PermissionCatalog.UserManage, PermissionCatalog.RoleManage, PermissionCatalog.PermissionManage, PermissionCatalog.WorkflowManage
        };
        foreach (var p in adminPermissions)
        {
            var rpId = $"rp-{roleId}-{p.ToString().Replace('.', '-')}";
            if (!await db.RolePermissions.AnyAsync(rp => rp.Id == rpId, cancellationToken))
                db.RolePermissions.Add(new RolePermission(rpId, tenantId, roleId, p));
        }

        const string employeeId = "emp-acme-admin";
        const string membershipId = "membership-emp-acme-admin";
        if (!await db.Employees.AnyAsync(e => e.TenantId == tenantId && e.Id == employeeId, cancellationToken))
            db.Employees.Add(new Employee(employeeId, tenantId, "Admin", "User", "admin@demo.com", null, "Administrator", 85000));
        if (!await db.UserMemberships.AnyAsync(m => m.Id == membershipId, cancellationToken))
            db.UserMemberships.Add(new UserMembership(membershipId, tenantId, employeeId));
        if (!await db.MembershipRoleAssignments.AnyAsync(a => a.TenantId == tenantId && a.MembershipId == membershipId && a.RoleId == roleId, cancellationToken))
            db.MembershipRoleAssignments.Add(new MembershipRoleAssignment(Guid.NewGuid().ToString("N"), tenantId, membershipId, roleId));

        var modules = new[]
        {
            EntitlementKeys.Modules.Assets, EntitlementKeys.Modules.Procurement, EntitlementKeys.Modules.Inventory,
            EntitlementKeys.Modules.Pos, EntitlementKeys.Modules.Finance, EntitlementKeys.Modules.Reporting,
            EntitlementKeys.Modules.Hr
        };
        foreach (var moduleKey in modules)
        {
            var assignmentId = $"ent-{tenantId}-{moduleKey.Namespace}-{moduleKey.Name}";
            if (!await db.EntitlementAssignments.AnyAsync(e => e.Id == assignmentId, cancellationToken))
            {
                db.EntitlementAssignments.Add(new EntitlementAssignment(
                    assignmentId, tenantId, moduleKey.Namespace, moduleKey.Name, EntitlementState.Active, "professional-plan"));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
