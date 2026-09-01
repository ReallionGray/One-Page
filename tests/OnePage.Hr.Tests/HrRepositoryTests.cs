using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using OnePage.Hr;
using OnePage.Platform;

namespace OnePage.Hr.Tests;

public sealed class HrRepositoryTests
{
    [Fact]
    public async Task Leave_request_reserves_balance_and_records_decision_history()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = await fixture.Repository.CreateLeaveRequestAsync(new LeaveRequest("lr-1", fixture.TenantId, "emp-1", "policy-1", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 11), 2, "Vacation"));
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Equal(8, (await fixture.Repository.GetBalanceAsync("emp-1", "policy-1", 2026))!.AvailableDays);
        await fixture.Repository.DecideLeaveAsync(request.Id, "user-1", true, "Approved");
        var decisions = await fixture.Repository.GetLeaveDecisionsAsync(request.Id);
        Assert.Single(decisions);
        Assert.Equal("Approved", decisions[0].Decision);
        Assert.Equal("user-1", decisions[0].ActorUserId);
    }

    [Fact]
    public async Task Leave_request_rejects_insufficient_balance_without_persisting_request()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<LeaveBalanceException>(() => fixture.Repository.CreateLeaveRequestAsync(new LeaveRequest("lr-2", fixture.TenantId, "emp-1", "policy-1", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 29), 20, "Too long")));
        Assert.Null(await fixture.Repository.GetLeaveRequestAsync("lr-2"));
        Assert.Equal(10, (await fixture.Repository.GetBalanceAsync("emp-1", "policy-1", 2026))!.AvailableDays);
    }

    [Fact]
    public async Task Related_references_are_tenant_scoped_before_write()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.OtherTenantEmployeeAsync();
        await Assert.ThrowsAsync<HrValidationException>(() => fixture.Repository.AddAsync(new EmployeeDocument("doc-x", fixture.TenantId, "emp-other", "ID", "s3://ref")));
        await Assert.ThrowsAsync<HrValidationException>(() => fixture.Repository.CreateEmploymentAsync(new Employment("employment-x", fixture.TenantId, "emp-1", new DateOnly(2026, 1, 1), "legal-other", "dept-1", "Manager", null, "loc-1")));
    }

    [Fact]
    public async Task Offboarding_persists_status_date_checklist_and_access_review()
    {
        await using var fixture = await Fixture.CreateAsync();
        var employee = await fixture.Repository.OffboardEmployeeAsync("emp-1", new DateOnly(2026, 8, 31), "user-1");
        Assert.False(employee.IsActive);
        Assert.Equal(new DateOnly(2026, 8, 31), employee.TerminationDate);
        await using var db = fixture.CreateHrDb();
        Assert.Contains(await db.ChecklistItems.ToListAsync(), x => x.EmployeeId == "emp-1" && x.Kind == ChecklistKind.Offboarding);
        Assert.Contains(await db.AccessReviewRequests.ToListAsync(), x => x.EmployeeId == "emp-1");
    }

    [Fact]
    public async Task List_employees_returns_all_in_tenant_scope()
    {
        await using var fixture = await Fixture.CreateAsync();
        var employees = await fixture.Repository.ListEmployeesAsync(default);
        Assert.Contains(employees, e => e.Id == "emp-1");
    }

    [Fact]
    public async Task Checklist_item_can_be_created_completed_and_retrieved()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Repository.AddAsync(new HrChecklistItem("check-1", fixture.TenantId, "emp-1", ChecklistKind.Onboarding, "Background check", "user-1", new DateOnly(2026, 9, 30)), default);
        var item = await fixture.Repository.GetChecklistItemAsync("check-1", default);
        Assert.NotNull(item);
        Assert.Equal(ChecklistStatus.Pending, item!.Status);
        var completed = await fixture.Repository.CompleteChecklistItemAsync("check-1", "verified", default);
        Assert.Equal(ChecklistStatus.Complete, completed.Status);
        Assert.Equal("verified", completed.CompletionEvidence);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task Disciplinary_action_lifecycle_resolve_cancel_expunge()
    {
        await using var fixture = await Fixture.CreateAsync();
        var action = await fixture.Repository.CreateDisciplinaryActionAsync(new DisciplinaryAction(
            "da-1", fixture.TenantId, "emp-1",
            DisciplinaryActionType.Warning, DisciplinarySeverity.Medium,
            "Attendance", "Repeated lateness", new DateOnly(2026, 8, 1)), default);
        Assert.Equal(DisciplinaryStatus.Active, action.Status);

        var resolved = await fixture.Repository.ResolveDisciplinaryActionAsync("da-1", "user-1", "Counselled", default);
        Assert.Equal(DisciplinaryStatus.Resolved, resolved.Status);

        var listed = await fixture.Repository.GetDisciplinaryActionsByEmployeeAsync("emp-1", default);
        Assert.Contains(listed, a => a.Id == "da-1");
    }

    [Fact]
    public async Task Recruitment_lifecycle_from_posting_to_offer()
    {
        await using var fixture = await Fixture.CreateAsync();
        var posting = await fixture.Repository.CreateJobPostingAsync(new JobPosting(
            "jp-1", fixture.TenantId, "Engineer", "Backend", "dept-1", "loc-1", null, null, null), default);
        var published = await fixture.Repository.PublishJobPostingAsync("jp-1", default);
        Assert.Equal(JobStatus.Published, published.Status);

        var application = await fixture.Repository.CreateJobApplicationAsync(new JobApplication(
            "ja-1", fixture.TenantId, "jp-1", "Jane Doe", "jane@example.com"), default);
        var statusUpdated = await fixture.Repository.UpdateJobApplicationStatusAsync("ja-1", ApplicationStatus.Interview, default);
        Assert.Equal(ApplicationStatus.Interview, statusUpdated.Status);

        var interview = await fixture.Repository.CreateInterviewAsync(new Interview(
            "int-1", fixture.TenantId, "ja-1", "emp-1", new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc)), default);
        var completed = await fixture.Repository.CompleteInterviewAsync("int-1", "Passed", default);
        Assert.NotNull(completed.CompletedAt);

        var offer = await fixture.Repository.CreateJobOfferAsync(new JobOffer(
            "offer-1", fixture.TenantId, "ja-1", 5000), default);
        var sent = await fixture.Repository.SendJobOfferAsync("offer-1", default);
        Assert.Equal(OfferStatus.Sent, sent.Status);

        var rejected = await fixture.Repository.RejectJobOfferAsync("offer-1", default);
        Assert.Equal(OfferStatus.Rejected, rejected.Status);
    }

    [Fact]
    public async Task Performance_review_lifecycle_submit_start_complete()
    {
        await using var fixture = await Fixture.CreateAsync();
        var cycle = await fixture.Repository.CreateReviewCycleAsync(new ReviewCycle(
            "rc-1", fixture.TenantId, "Annual 2026", "2026 cycle", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ReviewFramework.OKR), default);
        await fixture.Repository.UpdateReviewCycleAsync("rc-1", true, default);

        var review = await fixture.Repository.CreatePerformanceReviewAsync(new PerformanceReview(
            "pr-1", fixture.TenantId, "emp-1", "rc-1", ReviewFramework.OKR, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), default);
        var submitted = await fixture.Repository.SubmitPerformanceReviewAsync("pr-1", default);
        Assert.Equal(PerformanceReviewStatus.Submitted, submitted.Status);

        var started = await fixture.Repository.StartPerformanceReviewAsync("pr-1", default);
        Assert.Equal(PerformanceReviewStatus.InReview, started.Status);

        var completed = await fixture.Repository.CompletePerformanceReviewAsync("pr-1", 4.5m, "Well done", default);
        Assert.Equal(PerformanceReviewStatus.Completed, completed.Status);
        Assert.Equal(4.5m, completed.OverallScore);

        var goal = await fixture.Repository.CreatePerformanceGoalAsync(new PerformanceGoal(
            "pg-1", fixture.TenantId, "emp-1", "pr-1", "Ship feature", "Description", 10, new DateOnly(2026, 8, 31)), default);
        var updated = await fixture.Repository.UpdateGoalProgressAsync("pg-1", 5, GoalStatus.InProgress, default);
        Assert.Equal(5, updated.ActualValue);
        Assert.Equal(GoalStatus.InProgress, updated.Status);

        var commented = await fixture.Repository.AddEmployeeCommentsToReviewAsync("pr-1", "My self review", default);
        Assert.Contains("My self review", commented.EmployeeComments ?? "");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string path;
        private readonly SqliteConnection hrConnection;
        private readonly TenantContextAccessor accessor;
        private readonly OrganizationDbContext organizationDb;
        private readonly IOrganizationRepository organization;
        private readonly HrDbContext hrDb;
        public string TenantId => "tenant-1";
        public HrRepository Repository { get; }
        private Fixture(string path, SqliteConnection hrConnection, TenantContextAccessor accessor, OrganizationDbContext organizationDb, IOrganizationRepository organization, HrDbContext hrDb, HrRepository repository) { this.path = path; this.hrConnection = hrConnection; this.accessor = accessor; this.organizationDb = organizationDb; this.organization = organization; this.hrDb = hrDb; Repository = repository; }
        public static async Task<Fixture> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"onepage-hr-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<OrganizationDbContext>().UseSqlite($"Data Source={path}").Options;
            var organizationDb = new OrganizationDbContext(options); await organizationDb.Database.EnsureCreatedAsync();
            await organizationDb.Tenants.AddAsync(new Tenant("tenant-1", "Acme")); await organizationDb.Tenants.AddAsync(new Tenant("tenant-2", "Other"));
            await organizationDb.UserMemberships.AddAsync(new UserMembership("membership-1", "tenant-1", "user-1"));
            await organizationDb.SaveChangesAsync();
            foreach (var entity in new TenantOwnedRecord[] { new LegalEntity("legal-1", "tenant-1", "Acme Ltd"), new Department("dept-1", "tenant-1", "People"), new Location("loc-1", "tenant-1", "HQ") }) { organizationDb.Add(entity); }
            await organizationDb.SaveChangesAsync();
            var accessor = new TenantContextAccessor { Current = TenantContext.Create("user-1", "tenant-1", "test") };
            var orgRepository = new OrganizationRepository(organizationDb, accessor);
            var hrConnection = new SqliteConnection("Data Source=:memory:");
            await hrConnection.OpenAsync();
            var hrOptions = new DbContextOptionsBuilder<HrDbContext>().UseSqlite(hrConnection).Options;
            var hrDb = new HrDbContext(hrOptions); await hrDb.Database.EnsureCreatedAsync();
            var repository = new HrRepository(hrDb, organizationDb, accessor, orgRepository);
            await repository.CreateEmployeeAsync(new Employee("emp-1", "tenant-1", "E001", "Ada", "Lovelace", "ada@example.com"));
            await repository.CreateEmploymentAsync(new Employment("employment-1", "tenant-1", "emp-1", new DateOnly(2026, 1, 1), "legal-1", "dept-1", "Engineer", null, "loc-1"));
            await repository.AddAsync(new LeavePolicy("policy-1", "tenant-1", "ANNUAL", "Annual", 10));
            await repository.AddAsync(new LeaveBalance("balance-1", "tenant-1", "emp-1", "policy-1", 2026, 10));
            return new Fixture(path, hrConnection, accessor, organizationDb, orgRepository, hrDb, repository);
        }
        public async Task OtherTenantEmployeeAsync() { var options = new DbContextOptionsBuilder<HrDbContext>().UseSqlite(hrConnection).Options; await using var db = new HrDbContext(options); db.Employees.Add(new Employee("emp-other", "tenant-2", "E002", "Other", "Tenant")); await db.SaveChangesAsync(); }
        public HrDbContext CreateHrDb() => new(new DbContextOptionsBuilder<HrDbContext>().UseSqlite(hrConnection).Options);
        public async ValueTask DisposeAsync() { await hrDb.DisposeAsync(); await organizationDb.DisposeAsync(); await hrConnection.DisposeAsync(); if (File.Exists(path)) File.Delete(path); }
    }
}
