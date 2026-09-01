using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnePage.Api;
using OnePage.Hr;
using OnePage.Platform;

namespace OnePage.Hr.Tests;

public sealed class HrApiIntegrationTests
{
    [Fact]
    public async Task Authorized_employee_creation_and_read_redact_sensitive_fields()
    {
        await using var host = await ApiFixture.StartAsync();

        using var create = await host.Client.PostAsJsonAsync("/api/v1/hr/employees", new
        {
            Id = "emp-api-1", EmployeeNumber = "E-API-1", FirstName = "Grace", LastName = "Hopper",
            Email = "grace@example.com", Phone = "+2348000000000", GovernmentId = "NIN-SECRET"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var read = await host.Client.GetAsync("/api/v1/hr/employees/emp-api-1");
        var body = await read.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Contains("Grace", body);
        Assert.Contains("[redacted]", body);
        Assert.DoesNotContain("grace@example.com", body);
        Assert.DoesNotContain("+2348000000000", body);
        Assert.DoesNotContain("NIN-SECRET", body);
    }

    [Fact]
    public async Task HR_entitlement_denial_blocks_the_real_API_command()
    {
        await using var host = await ApiFixture.StartAsync(enableHr: false);

        using var response = await host.Client.PostAsJsonAsync("/api/v1/hr/employees", new
        {
            Id = "emp-api-entitlement", EmployeeNumber = "E-ENT", FirstName = "No", LastName = "Module"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("HR module unavailable", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Missing_permission_is_denied_at_the_API_boundary()
    {
        await using var host = await ApiFixture.StartAsync(permissionSet: []);

        using var response = await host.Client.PostAsJsonAsync("/api/v1/hr/employees", new
        {
            Id = "emp-api-permission", EmployeeNumber = "E-PERM", FirstName = "No", LastName = "Permission"
        });

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(nameof(AuthorizationDenialReason.MissingPermission), body);
    }

    [Fact]
    public async Task Leave_request_and_approval_use_the_real_API_path()
    {
        await using var host = await ApiFixture.StartAsync();
        await host.CreateEmployeeAndEmploymentAsync("emp-api-leave");
        await host.CreateLeavePolicyAndBalanceAsync("emp-api-leave");

        using var request = await host.Client.PostAsJsonAsync("/api/v1/hr/leave-requests", new
        {
            Id = "leave-api-1", EmployeeId = "emp-api-leave", PolicyId = "policy-api-1",
            StartDate = "2026-08-10", EndDate = "2026-08-11", Days = 2, Reason = "Annual leave"
        });
        Assert.Equal(HttpStatusCode.Created, request.StatusCode);

        using var decision = await host.Client.PostAsJsonAsync("/api/v1/hr/leave-requests/leave-api-1/decision", new
        {
            Approve = true, Comment = "Approved by manager"
        });
        var decisionBody = await decision.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        Assert.Contains("Approved", decisionBody);
        Assert.Contains("user-1", decisionBody);
    }

    [Fact]
    public async Task Offboarding_uses_the_real_API_path_and_retains_identity_record()
    {
        await using var host = await ApiFixture.StartAsync();
        await host.CreateEmployeeAndEmploymentAsync("emp-api-offboard");

        using var response = await host.Client.PostAsJsonAsync("/api/v1/hr/employees/emp-api-offboard/offboard", new
        {
            EffectiveDate = "2026-08-31"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"isActive\":false", body);
        Assert.Contains("2026-08-31", body);

        using var read = await host.Client.GetAsync("/api/v1/hr/employees/emp-api-offboard");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Contains("emp-api-offboard", await read.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cross_tenant_employee_reference_is_rejected_by_the_API_command()
    {
        await using var host = await ApiFixture.StartAsync();
        await host.CreateForeignEmployeeAsync("emp-api-foreign");

        using var response = await host.Client.PostAsJsonAsync("/api/v1/hr/leave-requests", new
        {
            Id = "leave-api-foreign", EmployeeId = "emp-api-foreign", PolicyId = "policy-api-1",
            StartDate = "2026-08-10", EndDate = "2026-08-11", Days = 2, Reason = "Must fail"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid HR request", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Disciplinary_action_full_lifecycle_through_API()
    {
        await using var host = await ApiFixture.StartAsync();
        await host.CreateEmployeeAndEmploymentAsync("emp-api-da");

        using var create = await host.Client.PostAsJsonAsync("/api/v1/hr/disciplinary-actions", new
        {
            Id = "da-api-1", EmployeeId = "emp-api-da", ActionType = "Warning", Severity = "Medium",
            Reason = "Attendance", Description = "Late", EffectiveDate = "2026-08-01"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var resolve = await host.Client.PostAsJsonAsync("/api/v1/hr/disciplinary-actions/da-api-1/resolve", new
        {
            ResolvedBy = "user-1", ResolutionNotes = "Counselled"
        });
        var resolveBody = await resolve.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);
        Assert.Contains("Counselled", resolveBody);
    }

    [Fact]
    public async Task Checklist_item_completion_through_API()
    {
        await using var host = await ApiFixture.StartAsync();
        await host.CreateEmployeeAndEmploymentAsync("emp-api-check");

        using var create = await host.Client.PostAsJsonAsync("/api/v1/hr/checklist-items", new
        {
            Id = "check-api-1", EmployeeId = "emp-api-check", Kind = "Onboarding",
            Title = "Background check", OwnerUserId = "user-1", DueDate = "2026-09-30"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var complete = await host.Client.PostAsJsonAsync("/api/v1/hr/checklist-items/check-api-1/complete", new
        {
            Evidence = "documents verified"
        });
        var completeBody = await complete.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Contains("documents verified", completeBody);
    }

    [Fact]
    public async Task Employment_creation_with_int_status_through_API()
    {
        await using var host = await ApiFixture.StartAsync();
        await host.CreateEmployeeAndEmploymentAsyncWithoutStatus("emp-api-employment-int");

        using var read = await host.Client.GetAsync("/api/v1/hr/employees/emp-api-employment-int/employment");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    private sealed class ApiFixture : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly string databasePath;

        private ApiFixture(WebApplication app, HttpClient client, string databasePath)
        {
            this.app = app;
            Client = client;
            this.databasePath = databasePath;
        }

        public HttpClient Client { get; }

        public static async Task<ApiFixture> StartAsync(bool enableHr = true, string[]? permissionSet = null)
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"onepage-hr-api-{Guid.NewGuid():N}.db");
            var permissions = permissionSet ??
            [
                PermissionCatalog.EmployeeCreate.ToString(), PermissionCatalog.EmployeeView.ToString(),
                PermissionCatalog.EmployeeUpdate.ToString(), PermissionCatalog.LeaveRequest.ToString(),
                PermissionCatalog.LeaveApprove.ToString(),
                PermissionCatalog.EmployeeTerminate.ToString(),
                PermissionCatalog.DisciplinaryManage.ToString()
            ];
            var app = ApiHost.Create(configureBuilder: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OnePage:ApiCredentials:key-1:UserId"] = "user-1",
                    ["OnePage:ApiCredentials:key-1:TenantIds:0"] = "tenant-1",
                    ["OnePage:DatabaseProvider"] = "sqlite",
                    ["ConnectionStrings:OnePage"] = $"Data Source={databasePath}"
                }));
            var entitlement = app.Services.GetRequiredService<InMemoryEntitlementEvaluator>();
            if (enableHr)
                entitlement.Set("tenant-1", new EntitlementDefinition(EntitlementKeys.Modules.Hr, EntitlementState.Active));

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            app.Urls.Add($"http://127.0.0.1:{port}");
            await app.StartAsync();
            await ApiHost.InitializeDatabaseAsync(app.Services);
            var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            client.DefaultRequestHeaders.Add("X-API-Key", "key-1");
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-1");
            var fixture = new ApiFixture(app, client, databasePath);
            await fixture.SeedAuthorizationAsync(permissions);
            return fixture;
        }

        public async Task CreateEmployeeAndEmploymentAsync(string employeeId)
        {
            using var employee = await Client.PostAsJsonAsync("/api/v1/hr/employees", new
            {
                Id = employeeId, EmployeeNumber = $"N-{employeeId}", FirstName = "Api", LastName = "Employee",
                Email = "private@example.com", GovernmentId = "PRIVATE"
            });
            Assert.Equal(HttpStatusCode.Created, employee.StatusCode);
            using var employment = await Client.PostAsJsonAsync($"/api/v1/hr/employees/{employeeId}/employment", new
            {
                Id = $"employment-{employeeId}", EffectiveFrom = "2026-01-01", LegalEntityId = "legal-1",
                DepartmentId = "dept-1", Position = "Engineer", LocationId = "loc-1", Status = 1
            });
            Assert.Equal(HttpStatusCode.Created, employment.StatusCode);
        }

        public async Task CreateEmployeeAndEmploymentAsyncWithoutStatus(string employeeId)
        {
            using var employee = await Client.PostAsJsonAsync("/api/v1/hr/employees", new
            {
                Id = employeeId, EmployeeNumber = $"N-{employeeId}", FirstName = "Api", LastName = "Employee",
                Email = "private@example.com", GovernmentId = "PRIVATE"
            });
            Assert.Equal(HttpStatusCode.Created, employee.StatusCode);
            using var employment = await Client.PostAsJsonAsync($"/api/v1/hr/employees/{employeeId}/employment", new
            {
                Id = $"employment-{employeeId}", EffectiveFrom = "2026-01-01", LegalEntityId = "legal-1",
                DepartmentId = "dept-1", Position = "Engineer", LocationId = "loc-1"
            });
            Assert.Equal(HttpStatusCode.Created, employment.StatusCode);
        }

        public async Task CreateLeavePolicyAndBalanceAsync(string employeeId)
        {
            using var policy = await Client.PostAsJsonAsync("/api/v1/hr/leave-policies", new
            {
                Id = "policy-api-1", Code = "ANNUAL", Name = "Annual", AnnualEntitlement = 10, AllowCarryover = false
            });
            Assert.Equal(HttpStatusCode.Created, policy.StatusCode);
            using var balance = await Client.PostAsJsonAsync("/api/v1/hr/leave-balances", new
            {
                Id = $"balance-{employeeId}", EmployeeId = employeeId, PolicyId = "policy-api-1", Year = 2026, EntitledDays = 10
            });
            Assert.Equal(HttpStatusCode.Created, balance.StatusCode);
        }

        public async Task CreateForeignEmployeeAsync(string employeeId)
        {
            var options = new DbContextOptionsBuilder<HrDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            await using var db = new HrDbContext(options);
            await HrPersistence.InitializeAsync(db);
            db.Employees.Add(new Employee(employeeId, "tenant-2", "FOREIGN", "Foreign", "Employee"));
            await db.SaveChangesAsync();
        }

        private async Task SeedAuthorizationAsync(IReadOnlyCollection<string> permissions)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            db.Tenants.Add(new Tenant("tenant-1", "Acme"));
            db.UserMemberships.Add(new UserMembership("membership-1", "tenant-1", "user-1"));
            db.Roles.Add(new Role("role-1", "tenant-1", "HR administrators"));
            foreach (var permission in permissions)
                db.RolePermissions.Add(new RolePermission($"permission-{Guid.NewGuid():N}", "tenant-1", "role-1", PermissionCatalog.Create(permission)));
            db.MembershipRoleAssignments.Add(new MembershipRoleAssignment("assignment-1", "tenant-1", "membership-1", "role-1"));
            db.LegalEntities.Add(new LegalEntity("legal-1", "tenant-1", "Acme Ltd"));
            db.Departments.Add(new Department("dept-1", "tenant-1", "People"));
            db.Locations.Add(new Location("loc-1", "tenant-1", "HQ"));
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
            if (File.Exists(databasePath)) File.Delete(databasePath);
            if (File.Exists($"{databasePath}-shm")) File.Delete($"{databasePath}-shm");
            if (File.Exists($"{databasePath}-wal")) File.Delete($"{databasePath}-wal");
        }
    }
}
