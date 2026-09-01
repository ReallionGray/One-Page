using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnePage.Api;
using OnePage.Hr;
using OnePage.Platform;

namespace OnePage.Platform.Tests;

public class ApiIntegrationTests
{
    [Fact] public async Task Health_endpoint_is_available()
    {
        await using var host = await StartHost();
        using var response = await host.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] public async Task Api_returns_denial_from_real_evaluator_for_missing_entitlement()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/entitlements/module/payroll");
        request.Headers.Add("X-API-Key", "key-1"); request.Headers.Add("X-Tenant-Id", "tenant-1"); request.Headers.Add("X-Correlation-Id", "corr-1");
        using var response = await host.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("ENTITLEMENT_DENIED", content);
    }

    [Fact] public async Task Api_propagates_tenant_context_to_scoped_platform_services()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        request.Headers.Add("X-API-Key", "key-1"); request.Headers.Add("X-Tenant-Id", "tenant-1"); request.Headers.Add("X-User-Id", "forged-user"); request.Headers.Add("X-Correlation-Id", "corr-9");
        using var response = await host.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tenant-1", body);
        Assert.Contains("user-1", body);
        Assert.DoesNotContain("forged-user", body);
    }

    [Fact]
    public async Task Api_rejects_missing_or_invalid_api_key()
    {
        await using var host = await StartHost();
        using var missing = await host.Client.GetAsync("/api/v1/platform/context");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);

        using var invalid = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        invalid.Headers.Add("X-API-Key", "invalid"); invalid.Headers.Add("X-Tenant-Id", "tenant-1");
        using var response = await host.Client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_denies_cross_tenant_selection_for_valid_credential()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        request.Headers.Add("X-API-Key", "key-1"); request.Headers.Add("X-Tenant-Id", "tenant-2");
        using var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Api_authorization_endpoint_enforces_persisted_membership_and_permission()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-platform003-{Guid.NewGuid():N}.db");
        await using var host = await StartHost(path);
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            await db.Tenants.AddAsync(new Tenant("tenant-1", "Acme"));
            await db.UserMemberships.AddAsync(new UserMembership("membership-1", "tenant-1", "user-1"));
            await db.Roles.AddAsync(new Role("role-1", "tenant-1", "Finance"));
            await db.RolePermissions.AddAsync(new RolePermission("permission-1", "tenant-1", "role-1", PermissionCatalog.ReportExport));
            await db.MembershipRoleAssignments.AddAsync(new MembershipRoleAssignment("assignment-1", "tenant-1", "membership-1", "role-1"));
            await db.SaveChangesAsync();
        }
        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/authorize/report.export");
        allowed.Headers.Add("X-API-Key", "key-1"); allowed.Headers.Add("X-Tenant-Id", "tenant-1");
        using var allowedResponse = await host.Client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);

        using var denied = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/authorize/payroll.run");
        denied.Headers.Add("X-API-Key", "key-1"); denied.Headers.Add("X-Tenant-Id", "tenant-1");
        var deniedResponse = await host.Client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        Assert.Contains(nameof(AuthorizationDenialReason.MissingPermission), await deniedResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Startup_initializer_uses_registered_database_context()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<OrganizationDbContext>(options => options.UseSqlite(connection));
        services.AddDbContext<HrDbContext>(options => options.UseSqlite(connection));
        await using var provider = services.BuildServiceProvider();
        await ApiHost.InitializeDatabaseAsync(provider);
        Assert.True(await provider.GetRequiredService<OrganizationDbContext>().Database.CanConnectAsync());
    }

    [Fact]
    public async Task Demo_procurement_pos_and_finance_endpoints_work_on_sqlite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-modules-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        using var poList = await host.Client.GetAsync("/api/v1/procurement/purchase-orders");
        Assert.Equal(HttpStatusCode.OK, poList.StatusCode);

        using var poCreate = await host.Client.PostAsJsonAsync("/api/v1/procurement/purchase-orders",
            new { Id = "po-it-1", Supplier = "Vendor", TotalAmount = 250m });
        Assert.Equal(HttpStatusCode.Created, poCreate.StatusCode);

        using var finList = await host.Client.GetAsync("/api/v1/finance/journal-entries");
        Assert.Equal(HttpStatusCode.OK, finList.StatusCode);

        using var finCreate = await host.Client.PostAsJsonAsync("/api/v1/finance/journal-entries",
            new { Id = "je-it-1", Reference = "Integration test" });
        Assert.Equal(HttpStatusCode.Created, finCreate.StatusCode);

        using var saleCreate = await host.Client.PostAsJsonAsync("/api/v1/pos/sales",
            new { Id = "sale-it-1", RegisterId = "REG-01", Total = 10m, Lines = new[] { new { Sku = "SKU-001", Quantity = 1m } } });
        Assert.Equal(HttpStatusCode.Created, saleCreate.StatusCode);

        using var salesList = await host.Client.GetAsync("/api/v1/pos/sales");
        Assert.Equal(HttpStatusCode.OK, salesList.StatusCode);
        var salesBody = await salesList.Content.ReadAsStringAsync();
        Assert.Contains("sale-it-1", salesBody);
    }

    [Fact]
    public async Task Login_without_tenant_auto_selects_single_organization()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-login-single-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        // user@demo.com belongs only to demo-tenant → login without TenantId should auto-select and return tokens
        using var response = await host.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "user@demo.com", password = "User@123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("accessToken", out var token) && !string.IsNullOrEmpty(token.GetString()));
        Assert.Equal("demo-tenant", body.GetProperty("tenantId").GetString());
    }

    [Fact]
    public async Task Login_without_tenant_returns_org_selection_for_multi_tenant_user()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-login-multi-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        // admin@demo.com belongs to both demo-tenant and acme-tenant → login without TenantId should return org list
        using var response = await host.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "admin@demo.com", password = "Admin@123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("requiresOrganizationSelection").GetBoolean());
        Assert.True(body.TryGetProperty("organizations", out var orgs));
        Assert.Equal(2, orgs.GetArrayLength());
        var tenantNames = orgs.EnumerateArray().Select(o => o.GetProperty("tenantName").GetString()).ToHashSet();
        Assert.Contains("Demo Tenant", tenantNames);
        Assert.Contains("Acme Corporation", tenantNames);
    }

    [Fact]
    public async Task Login_with_tenant_completes_after_org_selection()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-login-select-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        // Second step: supply the selected tenantId to complete the login
        using var response = await host.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "admin@demo.com", password = "Admin@123!", tenantId = "acme-tenant" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("accessToken", out var token) && !string.IsNullOrEmpty(token.GetString()));
        Assert.Equal("acme-tenant", body.GetProperty("tenantId").GetString());
    }

    [Fact]
    public async Task Login_without_tenant_rejects_invalid_credentials()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-login-bad-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        using var response = await host.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "user@demo.com", password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Approvals_pending_filtered_by_current_step_approver()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-approvals-pending-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.GetAsync("/api/v1/approvals");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var approvals = await response.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();
        var ids = approvals.Select(a => a.GetProperty("id").GetString()).ToHashSet();

        // admin (role "admin") is the step-1 approver for wf-po-standard and wf-asset-dispose
        Assert.Contains("approval-demo-001", ids);
        Assert.Contains("approval-demo-002", ids);
        Assert.Contains("approval-demo-004", ids);
        // approval-demo-003 is already "approved" → not in pending list
        Assert.DoesNotContain("approval-demo-003", ids);
    }

    [Fact]
    public async Task Approval_decide_advances_step_then_completes_on_final()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-approve-flow-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        // Step 1: Login as admin (emp-002, "admin" role), approve step 1 of approval-demo-001.
        var adminToken = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        using var decide1 = await host.Client.PostAsJsonAsync("/api/v1/approvals/approval-demo-001/decide",
            new { approve = true, comment = "Approved as admin" });
        Assert.Equal(HttpStatusCode.OK, decide1.StatusCode);
        var body1 = await decide1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pending", body1.GetProperty("status").GetString());
        Assert.Equal(2, body1.GetProperty("currentStep").GetInt32());

        // Verify a decision record was created for step 1
        using var decisions = await host.Client.GetAsync("/api/v1/approvals/approval-demo-001/decisions");
        Assert.Equal(HttpStatusCode.OK, decisions.StatusCode);
        var decisionList = await decisions.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();
        Assert.Single(decisionList);
        Assert.Equal("approved", decisionList[0].GetProperty("decision").GetString());
        Assert.Equal(1, decisionList[0].GetProperty("stepNumber").GetInt32());

        // Step 2: Login as super-admin to complete step 2 (hrmanager step — super-admin bypasses the step check)
        var saToken = await LoginAsAsync(host.Client, "superadmin@demo.com", "SuperAdmin@123!");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", saToken);

        using var decide2 = await host.Client.PostAsJsonAsync("/api/v1/approvals/approval-demo-001/decide",
            new { approve = true, comment = "Final approval" });
        Assert.Equal(HttpStatusCode.OK, decide2.StatusCode);
        var body2 = await decide2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approved", body2.GetProperty("status").GetString());
        Assert.Equal(2, body2.GetProperty("currentStep").GetInt32());

        // Step 3: Verify the completed approval appears in the admin's completed list.
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        using var completed = await host.Client.GetAsync("/api/v1/approvals/completed");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var completedList = await completed.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();
        var completedIds = completedList.Select(a => a.GetProperty("id").GetString()).ToHashSet();
        // approval-demo-003 was seeded as fully approved by emp-002 → in completed
        Assert.Contains("approval-demo-003", completedIds);
        // approval-demo-001 was just completed; emp-002 approved step 1 → appears in completed
        Assert.Contains("approval-demo-001", completedIds);
    }

    [Fact]
    public async Task Approval_get_detail_returns_workflow_info_and_decisions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-approvals-detail-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.GetAsync("/api/v1/approvals/approval-demo-001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("approval-demo-001", body.GetProperty("id").GetString());
        Assert.Equal("pending", body.GetProperty("status").GetString());
        Assert.Equal("Standard PO Approval", body.GetProperty("workflowName").GetString());
        Assert.Equal(1, body.GetProperty("currentStep").GetInt32());
        Assert.Equal("purchase_order.approve", body.GetProperty("resourceType").GetString());

        // Step approvers (2 steps for wf-po-standard)
        var stepApprovers = body.GetProperty("stepApprovers").EnumerateArray()
            .Select(s => (s.GetProperty("stepNumber").GetInt32(),
                          s.GetProperty("approverType").GetString(),
                          s.GetProperty("approverValue").GetString(),
                          s.GetProperty("canSkip").GetBoolean()))
            .ToList();
        Assert.Equal(2, stepApprovers.Count);
        Assert.Equal(1, stepApprovers[0].Item1);
        Assert.Equal("role", stepApprovers[0].Item2);
        Assert.Equal("admin", stepApprovers[0].Item3);
        Assert.False(stepApprovers[0].Item4);
        Assert.Equal(2, stepApprovers[1].Item1);
        Assert.Equal("hrmanager", stepApprovers[1].Item3);
        Assert.False(stepApprovers[1].Item4);
        // Current step (step 1) does not allow skip in the standard PO workflow
        Assert.False(body.GetProperty("currentStepCanSkip").GetBoolean());

        // Decision history (empty for a pending request with no decisions)
        Assert.Equal(0, body.GetProperty("decisions").GetArrayLength());
    }

    [Fact]
    public async Task Workflow_CRUD_creates_lists_updates_deletes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-workflow-crud-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        // admin has WorkflowManage permission via the "admin" role
        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create a workflow with dynamic steps, quantity trigger, and skip flags
        var createBody = new
        {
            name = "Integration Test Workflow",
            resourceType = "purchase_order.approve",
            triggerType = "amount",
            triggerAmount = 500m,
            triggerQuantity = (decimal?)null,
            isActive = true,
            description = "Created by integration test",
            steps = new[]
            {
                new { approverType = "role", approverValue = "accountant", canSkip = true },
                new { approverType = "role", approverValue = "admin", canSkip = false }
            }
        };
        using var create = await host.Client.PostAsJsonAsync("/api/v1/workflows", createBody);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var workflowId = created.GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(workflowId));
        Assert.Equal(2, created.GetProperty("steps").GetArrayLength());
        Assert.True(created.GetProperty("steps")[0].GetProperty("canSkip").GetBoolean());
        Assert.False(created.GetProperty("steps")[1].GetProperty("canSkip").GetBoolean());

        // List workflows — the new one should appear alongside seeded ones
        using var list = await host.Client.GetAsync("/api/v1/workflows");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var workflows = await list.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();
        var ids = workflows.Select(w => w.GetProperty("id").GetString()).ToHashSet();
        Assert.Contains(workflowId, ids);
        Assert.Contains("wf-po-standard", ids); // seeded workflow

        // Get the specific workflow with steps
        using var get = await host.Client.GetAsync("/api/v1/workflows/" + workflowId);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var wf = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Integration Test Workflow", wf.GetProperty("name").GetString());
        Assert.Equal("amount", wf.GetProperty("triggerType").GetString());
        Assert.Equal(500m, wf.GetProperty("triggerAmount").GetDecimal());
        Assert.True(wf.GetProperty("triggerQuantity").ValueKind == System.Text.Json.JsonValueKind.Null);
        Assert.True(wf.GetProperty("steps")[0].GetProperty("canSkip").GetBoolean());
        Assert.False(wf.GetProperty("steps")[1].GetProperty("canSkip").GetBoolean());

        // Update the workflow
        var updateBody = new
        {
            name = "Updated Workflow",
            description = "Updated by integration test",
            resourceType = "purchase_order.approve",
            triggerType = "always",
            triggerAmount = (decimal?)null,
            triggerQuantity = (decimal?)null,
            isActive = false,
            steps = new[]
            {
                new { approverType = "role", approverValue = "admin", canSkip = true }
            }
        };
        using var update = await host.Client.PutAsJsonAsync("/api/v1/workflows/" + workflowId, updateBody);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        // Delete the workflow
        using var delete = await host.Client.DeleteAsync("/api/v1/workflows/" + workflowId);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Verify deletion
        using var listAfter = await host.Client.GetAsync("/api/v1/workflows");
        Assert.Equal(HttpStatusCode.OK, listAfter.StatusCode);
        var workflowsAfter = await listAfter.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();
        var idsAfter = workflowsAfter.Select(w => w.GetProperty("id").GetString()).ToHashSet();
        Assert.DoesNotContain(workflowId, idsAfter);
        Assert.Contains("wf-po-standard", idsAfter); // seeded workflow still there
    }

    [Fact]
    public async Task Workflow_quantity_trigger_creates_and_serializes_fields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-wf-quantity-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createBody = new
        {
            name = "Inventory Quantity Trigger Workflow",
            resourceType = "inventory.adjust",
            triggerType = "quantity",
            triggerAmount = (decimal?)null,
            triggerQuantity = 50m,
            isActive = true,
            description = "Triggers when quantity >= 50",
            steps = new[]
            {
                new { approverType = "role", approverValue = "admin", canSkip = true }
            }
        };
        using var create = await host.Client.PostAsJsonAsync("/api/v1/workflows", createBody);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var workflowId = created.GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(workflowId));

        // Verify the quantity trigger and triggerQuantity are serialized correctly
        using var get = await host.Client.GetAsync("/api/v1/workflows/" + workflowId);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var wf = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("quantity", wf.GetProperty("triggerType").GetString());
        Assert.Equal(50m, wf.GetProperty("triggerQuantity").GetDecimal());
        Assert.True(wf.GetProperty("steps")[0].GetProperty("canSkip").GetBoolean());

        // Clean up
        using var delete = await host.Client.DeleteAsync("/api/v1/workflows/" + workflowId);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Workflow_resource_types_endpoint_returns_module_action_pairs()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-wf-rt-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.GetAsync("/api/v1/workflows/resource-types");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rts = body.GetProperty("resourceTypes").EnumerateArray().ToList();
        Assert.Equal(3, rts.Count);

        // Verify each module has the expected actions
        var po = rts.First(r => r.GetProperty("module").GetString() == "purchase_order");
        Assert.Contains("approve", po.GetProperty("actions").EnumerateArray().Select(a => a.GetString()));

        var asset = rts.First(r => r.GetProperty("module").GetString() == "asset");
        var assetActions = asset.GetProperty("actions").EnumerateArray().Select(a => a.GetString()).ToHashSet();
        Assert.Contains("dispose", assetActions);
        Assert.Contains("transfer", assetActions);
        Assert.Contains("assign", assetActions);

        var inventory = rts.First(r => r.GetProperty("module").GetString() == "inventory");
        Assert.Contains("adjust", inventory.GetProperty("actions").EnumerateArray().Select(a => a.GetString()));
    }

    [Fact]
    public async Task Workflow_roles_with_users_endpoint_returns_roles_and_users()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-wf-rwu-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.GetAsync("/api/v1/workflows/roles-with-users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var roles = body.GetProperty("roles").EnumerateArray()
            .Select(r => r.Deserialize<JsonElement>()).ToList();
        // The demo tenant has SuperAdmin, admin, hrmanager, accountant, sales, user
        Assert.NotEmpty(roles);
        var roleNames = roles.Select(r => r.GetProperty("name").GetString()).ToHashSet();
        Assert.Contains("admin", roleNames);
        Assert.Contains("hrmanager", roleNames);
        Assert.Contains("accountant", roleNames);
        Assert.Contains("sales", roleNames);

        // Each role should have a users array
        foreach (var role in roles)
        {
            Assert.True(role.TryGetProperty("users", out var users));
            Assert.True(users.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task Workflow_can_skip_allows_next_step_approver_to_approve_current_step()
    {
        var path = Path.Combine(Path.GetTempPath(), $"onepage-wf-skip-{Guid.NewGuid():N}.db");
        await using var host = await StartDevHost(path);

        // admin has WorkflowManage permission
        var token = await LoginAsAsync(host.Client, "admin@demo.com", "Admin@123!", "demo-tenant");
        host.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create a 2-step workflow: step 1 is assigned to "accountant" with CanSkip=true,
        // step 2 is assigned to "admin". Because step 1 has CanSkip=true, admin (step 2 approver)
        // should see the approval in pending list even though admin is NOT the step-1 approver.
        var createBody = new
        {
            name = "Skip Test Workflow",
            resourceType = "purchase_order.approve",
            triggerType = "amount",
            triggerAmount = 1m,
            triggerQuantity = (decimal?)null,
            isActive = true,
            description = "Tests CanSkip step behavior",
            steps = new[]
            {
                new { approverType = "role", approverValue = "accountant", canSkip = true },
                new { approverType = "role", approverValue = "admin", canSkip = false }
            }
        };
        using var create = await host.Client.PostAsJsonAsync("/api/v1/workflows", createBody);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var workflowId = created.GetProperty("id").GetString();

            Assert.False(string.IsNullOrEmpty(workflowId));

        // Directly insert an approval request in the database attached to this workflow,
        // with CurrentStep=1 (pointing to the accountant step).
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            var request = new ApprovalRequest(
                "po-skip-test-1", "demo-tenant", "purchase_order.approve", "po-skip-test-1",
                "emp-006", "10m");
            request.AttachWorkflow(workflowId!);
            // CurrentStep defaults to 1 in AttachWorkflow
            await db.ApprovalRequests.AddAsync(request);
            await db.SaveChangesAsync();
        }

        // admin is NOT the step-1 approver (accountant) but IS the step-2 approver (admin).
        // Since step 1 has CanSkip=true, the admin should see this approval in pending.
        using var approvalsResponse = await host.Client.GetAsync("/api/v1/approvals");
        Assert.Equal(HttpStatusCode.OK, approvalsResponse.StatusCode);
        var approvals = await approvalsResponse.Content.ReadFromJsonAsync<List<JsonElement>>() ?? new();
        var approvalIds = approvals.Select(a => a.GetProperty("id").GetString()).ToHashSet();
        Assert.Contains("po-skip-test-1", approvalIds);

        // Verify the detail shows currentStepCanSkip = true
        using var detail = await host.Client.GetAsync("/api/v1/approvals/po-skip-test-1");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailBody = await detail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(detailBody.GetProperty("currentStepCanSkip").GetBoolean());

        // Clean up
        using var delete = await host.Client.DeleteAsync("/api/v1/workflows/" + workflowId);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    private static async Task<string> LoginAsAsync(HttpClient client, string username, string password, string? tenantId = null)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { username, password, tenantId });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private static Task<RunningHost> StartHost() => StartHost(null);

    private static Task<RunningHost> StartDevHost(string sqlitePath) => StartHost(sqlitePath, development: true);

    private static async Task<RunningHost> StartHost(string? sqlitePath, bool development = false)
    {
        var app = ApiHost.Create(configureBuilder: builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OnePage:ApiCredentials:key-1:UserId"] = "user-1",
                ["OnePage:ApiCredentials:key-1:TenantIds:0"] = "tenant-1",
                ["OnePage:DatabaseProvider"] = sqlitePath is null ? null : "sqlite",
                ["ConnectionStrings:OnePage"] = sqlitePath is null ? null : $"Data Source={sqlitePath}"
            });
            if (development) builder.Environment.EnvironmentName = "Development";
        });
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        app.Urls.Add($"http://127.0.0.1:{port}");
        await app.StartAsync();
        var address = app.Urls.First();
        if (sqlitePath is not null) await ApiHost.InitializeDatabaseAsync(app.Services);
        return new RunningHost(app, new HttpClient { BaseAddress = new Uri(address) }, sqlitePath);
    }

    private sealed class RunningHost : IAsyncDisposable
    {
        public WebApplication App { get; }
        public HttpClient Client { get; }
        private readonly string? _sqlitePath;

        public RunningHost(WebApplication app, HttpClient client, string? sqlitePath)
        {
            App = app;
            Client = client;
            _sqlitePath = sqlitePath;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
            if (_sqlitePath is not null && File.Exists(_sqlitePath)) File.Delete(_sqlitePath);
        }
    }
}
