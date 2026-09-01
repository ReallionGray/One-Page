using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using OnePage.Platform;

namespace OnePage.Hr;

public interface IHrRepository
{
    Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken ct = default);
    Task<Employee?> GetEmployeeAsync(string id, CancellationToken ct = default);
    Task<Employment?> GetCurrentEmploymentAsync(string employeeId, CancellationToken ct = default);
    Task<Employment> CreateEmploymentAsync(Employment employment, CancellationToken ct = default);
    Task<IReadOnlyList<Employment>> GetEmploymentsAsync(string employeeId, CancellationToken ct = default);
    Task<T> AddAsync<T>(T record, CancellationToken ct = default) where T : class;
    Task<EmployeeDocument?> GetDocumentAsync(string id, CancellationToken ct = default);
    Task<LeaveBalance?> GetBalanceAsync(string employeeId, string policyId, int year, CancellationToken ct = default);
    Task<LeaveRequest?> GetLeaveRequestAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveDecision>> GetLeaveDecisionsAsync(string requestId, CancellationToken ct = default);
    Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default);
    Task<LeaveDecision> DecideLeaveAsync(string requestId, string actorUserId, bool approve, string? comment, CancellationToken ct = default);
    Task<Employee> OffboardEmployeeAsync(string employeeId, DateOnly effectiveDate, string actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> ListEmployeesAsync(CancellationToken ct = default);
    
    // Performance management methods
    Task<PerformanceReview> CreatePerformanceReviewAsync(PerformanceReview review, CancellationToken ct = default);
    Task<PerformanceReview?> GetPerformanceReviewAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<PerformanceReview>> GetPerformanceReviewsByEmployeeAsync(string employeeId, CancellationToken ct = default);
    Task<PerformanceGoal> CreatePerformanceGoalAsync(PerformanceGoal goal, CancellationToken ct = default);
    Task<PerformanceGoal?> GetPerformanceGoalAsync(string goalId, CancellationToken ct = default);
    Task<IReadOnlyList<PerformanceGoal>> GetPerformanceGoalsByReviewAsync(string reviewId, CancellationToken ct = default);
    Task<PerformanceFeedback> CreatePerformanceFeedbackAsync(PerformanceFeedback feedback, CancellationToken ct = default);
    Task<IReadOnlyList<PerformanceFeedback>> GetPerformanceFeedbacksByReviewAsync(string reviewId, CancellationToken ct = default);
    Task<CompetencyAssessment> CreateCompetencyAssessmentAsync(CompetencyAssessment assessment, CancellationToken ct = default);
    Task<IReadOnlyList<CompetencyAssessment>> GetCompetencyAssessmentsByReviewAsync(string reviewId, CancellationToken ct = default);
    Task<ReviewCycle> CreateReviewCycleAsync(ReviewCycle cycle, CancellationToken ct = default);
    Task<IReadOnlyList<ReviewCycle>> GetActiveReviewCyclesAsync(string tenantId, CancellationToken ct = default);
    Task<AppraisalCommittee> CreateAppraisalCommitteeAsync(AppraisalCommittee committee, CancellationToken ct = default);
    
    // Recruitment methods
    Task<JobPosting> CreateJobPostingAsync(JobPosting posting, CancellationToken ct = default);
    Task<JobPosting?> GetJobPostingAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<JobPosting>> GetJobPostingsByStatusAsync(string tenantId, JobStatus status, CancellationToken ct = default);
    Task<JobApplication> CreateJobApplicationAsync(JobApplication application, CancellationToken ct = default);
    Task<JobApplication?> GetJobApplicationAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<JobApplication>> GetJobApplicationsByPostingAsync(string jobPostingId, CancellationToken ct = default);
    Task<Interview> CreateInterviewAsync(Interview interview, CancellationToken ct = default);
    Task<IReadOnlyList<Interview>> GetInterviewsByApplicationAsync(string jobApplicationId, CancellationToken ct = default);
    Task<JobOffer> CreateJobOfferAsync(JobOffer offer, CancellationToken ct = default);
    Task<JobOffer?> GetJobOfferAsync(string id, CancellationToken ct = default);

    // Disciplinary management methods
    Task<DisciplinaryAction> CreateDisciplinaryActionAsync(DisciplinaryAction action, CancellationToken ct = default);
    Task<DisciplinaryAction?> GetDisciplinaryActionAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<DisciplinaryAction>> GetDisciplinaryActionsByEmployeeAsync(string employeeId, CancellationToken ct = default);
    Task<DisciplinaryAction> ResolveDisciplinaryActionAsync(string actionId, string resolvedBy, string? resolutionNotes, CancellationToken ct = default);
    Task<DisciplinaryAction> ExpungeDisciplinaryActionAsync(string actionId, CancellationToken ct = default);
    Task<DisciplinaryAction> CancelDisciplinaryActionAsync(string actionId, CancellationToken ct = default);

    // Recruitment lifecycle methods
    Task<JobPosting> PublishJobPostingAsync(string jobPostingId, CancellationToken ct = default);
    Task<JobPosting> CloseJobPostingAsync(string jobPostingId, CancellationToken ct = default);
    Task<JobApplication> UpdateJobApplicationStatusAsync(string applicationId, ApplicationStatus status, CancellationToken ct = default);
    Task<Interview> CompleteInterviewAsync(string interviewId, string? notes, CancellationToken ct = default);
    Task<Interview> CancelInterviewAsync(string interviewId, CancellationToken ct = default);
    Task<JobOffer> SendJobOfferAsync(string offerId, CancellationToken ct = default);
    Task<JobOffer> AcceptJobOfferAsync(string offerId, CancellationToken ct = default);
    Task<JobOffer> RejectJobOfferAsync(string offerId, CancellationToken ct = default);
    Task<JobOffer> WithdrawJobOfferAsync(string offerId, CancellationToken ct = default);

    // Performance lifecycle methods
    Task<PerformanceReview> SubmitPerformanceReviewAsync(string reviewId, CancellationToken ct = default);
    Task<PerformanceReview> StartPerformanceReviewAsync(string reviewId, CancellationToken ct = default);
    Task<PerformanceReview> CompletePerformanceReviewAsync(string reviewId, decimal overallScore, string? managerComments, CancellationToken ct = default);
    Task<PerformanceReview> AddEmployeeCommentsToReviewAsync(string reviewId, string comments, CancellationToken ct = default);
    Task<PerformanceGoal> UpdateGoalProgressAsync(string goalId, decimal actualValue, GoalStatus status, CancellationToken ct = default);
    Task<ReviewCycle> UpdateReviewCycleAsync(string cycleId, bool isActive, CancellationToken ct = default);

    // Checklist lifecycle methods
    Task<HrChecklistItem?> GetChecklistItemAsync(string id, CancellationToken ct = default);
    Task<HrChecklistItem> CompleteChecklistItemAsync(string itemId, string evidence, CancellationToken ct = default);

    // Payroll enhancement methods
    Task<HrPayrollRecord> CreatePayrollRecordAsync(HrPayrollRecord record, CancellationToken ct = default);
    Task<HrPayrollRecord?> GetPayrollRecordAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<HrPayrollRecord>> GetPayrollRecordsByEmployeeAsync(string employeeId, CancellationToken ct = default);
    Task<HrPayrollRecord> ProcessPayrollRecordAsync(string recordId, CancellationToken ct = default);
    Task<EmployeeLoan> CreateEmployeeLoanAsync(EmployeeLoan loan, CancellationToken ct = default);
    Task<EmployeeLoan?> GetEmployeeLoanAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeLoan>> GetEmployeeLoansAsync(string employeeId, CancellationToken ct = default);
    Task<EmployeeLoan> RepayEmployeeLoanAsync(string loanId, decimal amount, CancellationToken ct = default);

    // Advanced time and attendance methods
    Task<TimeEntry> CreateTimeEntryAsync(TimeEntry entry, CancellationToken ct = default);
    Task<TimeEntry?> GetTimeEntryAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<TimeEntry>> GetTimeEntriesByEmployeeAsync(string employeeId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default);
    Task<TimeEntry> ClockOutTimeEntryAsync(string entryId, DateTimeOffset clockOutTime, string? notes = null, CancellationToken ct = default);
    Task<WorkSchedule> CreateWorkScheduleAsync(WorkSchedule schedule, CancellationToken ct = default);
    Task<IReadOnlyList<WorkSchedule>> GetWorkSchedulesByEmployeeAsync(string employeeId, CancellationToken ct = default);
    Task<OvertimeRequest> CreateOvertimeRequestAsync(OvertimeRequest request, CancellationToken ct = default);
    Task<OvertimeRequest?> GetOvertimeRequestAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<OvertimeRequest>> GetOvertimeRequestsByEmployeeAsync(string employeeId, CancellationToken ct = default);
    Task<OvertimeRequest> ApproveOvertimeRequestAsync(string requestId, string approvedBy, CancellationToken ct = default);
    Task<OvertimeRequest> RejectOvertimeRequestAsync(string requestId, CancellationToken ct = default);
}

public sealed class HrRepository(HrDbContext db, OrganizationDbContext organizationDb, ITenantContextAccessor context, IOrganizationRepository organization) : IHrRepository
{
    private string TenantId => context.Current?.TenantId ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");

    public async Task<Employee> CreateEmployeeAsync(Employee employee, CancellationToken ct = default)
    { Ensure(employee.TenantId); db.Employees.Add(employee); await db.SaveChangesAsync(ct); return employee; }

    public Task<Employee?> GetEmployeeAsync(string id, CancellationToken ct = default) =>
        db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<Employee>> ListEmployeesAsync(CancellationToken ct = default) =>
        await db.Employees.AsNoTracking().Where(x => x.TenantId == TenantId).ToListAsync(ct);

    public Task<Employment?> GetCurrentEmploymentAsync(string employeeId, CancellationToken ct = default) =>
        db.Employments.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);

    public async Task<Employment> CreateEmploymentAsync(Employment employment, CancellationToken ct = default)
    {
        Ensure(employment.TenantId);
        await RequireEmployeeAsync(employment.EmployeeId, ct);
        await RequireOrganizationAsync<LegalEntity>(employment.LegalEntityId, nameof(employment.LegalEntityId), ct);
        await RequireOrganizationAsync<Department>(employment.DepartmentId, nameof(employment.DepartmentId), ct);
        await RequireOrganizationAsync<Location>(employment.LocationId, nameof(employment.LocationId), ct);
        if (employment.ManagerEmployeeId is not null)
        {
            if (employment.ManagerEmployeeId == employment.EmployeeId) throw new HrValidationException(nameof(employment.ManagerEmployeeId), "An employee cannot manage themselves.");
            await RequireEmployeeAsync(employment.ManagerEmployeeId, ct);
        }
        var overlap = await db.Employments.AnyAsync(x => x.TenantId == TenantId && x.EmployeeId == employment.EmployeeId &&
            x.EffectiveFrom <= (employment.EffectiveTo ?? DateOnly.MaxValue) && (x.EffectiveTo ?? DateOnly.MaxValue) >= employment.EffectiveFrom, ct);
        if (overlap) throw new HrValidationException(nameof(employment.EffectiveFrom), "Employment dates overlap an existing employment period.");
        db.Employments.Add(employment); await db.SaveChangesAsync(ct); return employment;
    }

    public async Task<IReadOnlyList<Employment>> GetEmploymentsAsync(string employeeId, CancellationToken ct = default) =>
        await db.Employments.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderBy(x => x.EffectiveFrom).ToListAsync(ct);

    public async Task<T> AddAsync<T>(T record, CancellationToken ct = default) where T : class
    {
        var tenant = (string?)db.Entry(record).Property("TenantId").CurrentValue; Ensure(tenant);
        switch (record)
        {
            case HrChecklistItem checklist: await RequireEmployeeAsync(checklist.EmployeeId, ct); await RequireMembershipOwnerAsync(checklist.OwnerUserId, ct); break;
            case LeaveBalance balance: await RequireEmployeeAsync(balance.EmployeeId, ct); await RequirePolicyAsync(balance.PolicyId, ct); break;
            case EmployeeDocument document: await RequireEmployeeAsync(document.EmployeeId, ct); break;
            case LeaveDecision decision: await RequireLeaveRequestAsync(decision.LeaveRequestId, ct); break;
            case HrAccessReviewRequest review: await RequireEmployeeAsync(review.EmployeeId, ct); break;
        }
        db.Add(record); await db.SaveChangesAsync(ct); return record;
    }

    public Task<EmployeeDocument?> GetDocumentAsync(string id, CancellationToken ct = default) =>
        db.EmployeeDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public Task<LeaveBalance?> GetBalanceAsync(string employeeId, string policyId, int year, CancellationToken ct = default) =>
        db.LeaveBalances.SingleOrDefaultAsync(x => x.EmployeeId == Required(employeeId) && x.PolicyId == Required(policyId) && x.Year == year && x.TenantId == TenantId, ct);

    public Task<LeaveRequest?> GetLeaveRequestAsync(string id, CancellationToken ct = default) =>
        db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<LeaveDecision>> GetLeaveDecisionsAsync(string requestId, CancellationToken ct = default) =>
        (await db.LeaveDecisions.AsNoTracking().Where(x => x.LeaveRequestId == Required(requestId) && x.TenantId == TenantId).ToListAsync(ct))
        .OrderBy(x => x.DecidedAt)
        .ThenBy(x => x.Id)
        .ToList();

    public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default)
    {
        Ensure(request.TenantId);
        await RequireEmployeeAsync(request.EmployeeId, ct);
        var policy = await RequirePolicyAsync(request.PolicyId, ct);
        if (request.StartDate.Year != request.EndDate.Year) throw new HrValidationException(nameof(request.EndDate), "A leave request must remain within one calendar year.");
        if (request.Days > request.EndDate.DayNumber - request.StartDate.DayNumber + 1) throw new HrValidationException(nameof(request.Days), "Leave days exceed the requested date range.");
        if (request.Days > policy.AnnualEntitlement) throw new LeaveBalanceException("Requested leave exceeds the policy entitlement.");
        var balance = await db.LeaveBalances.SingleOrDefaultAsync(x => x.TenantId == TenantId && x.EmployeeId == request.EmployeeId && x.PolicyId == request.PolicyId && x.Year == request.StartDate.Year, ct)
            ?? throw new LeaveBalanceException("No leave balance exists for the requested employee, policy, and year.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        balance.Reserve(request.Days);
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return request;
    }

    public async Task<LeaveDecision> DecideLeaveAsync(string requestId, string actorUserId, bool approve, string? comment, CancellationToken ct = default)
    {
        await RequireMembershipOwnerAsync(actorUserId, ct);
        var request = await db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == Required(requestId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(requestId), "Leave request does not exist in the current tenant.");
        if (approve) request.Approve(); else request.Reject();
        var decision = new LeaveDecision(Guid.NewGuid().ToString("N"), TenantId, request.Id, actorUserId, approve ? "Approved" : "Rejected", comment);
        db.LeaveDecisions.Add(decision);
        await db.SaveChangesAsync(ct); return decision;
    }

    public async Task<Employee> OffboardEmployeeAsync(string employeeId, DateOnly effectiveDate, string actorUserId, CancellationToken ct = default)
    {
        await RequireMembershipOwnerAsync(actorUserId, ct);
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.Id == Required(employeeId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(employeeId), "Employee does not exist in the current tenant.");
        employee.Offboard(effectiveDate);
        var employment = await db.Employments.Where(x => x.TenantId == TenantId && x.EmployeeId == employee.Id).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        employment?.ChangeStatus(EmploymentStatus.Terminated);
        db.ChecklistItems.Add(new HrChecklistItem(Guid.NewGuid().ToString("N"), TenantId, employee.Id, ChecklistKind.Offboarding, "Complete offboarding access and asset review", actorUserId, effectiveDate));
        db.AccessReviewRequests.Add(new HrAccessReviewRequest(Guid.NewGuid().ToString("N"), TenantId, employee.Id, actorUserId, effectiveDate));
        await db.SaveChangesAsync(ct); return employee;
    }

    private async Task<Employee> RequireEmployeeAsync(string id, CancellationToken ct) => await db.Employees.SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct) ?? throw new HrValidationException(nameof(id), "Employee reference does not exist in the current tenant.");
    private async Task<LeavePolicy> RequirePolicyAsync(string id, CancellationToken ct) => await db.LeavePolicies.SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct) ?? throw new HrValidationException(nameof(id), "Leave policy reference does not exist in the current tenant.");
    private async Task<LeaveRequest> RequireLeaveRequestAsync(string id, CancellationToken ct) => await db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct) ?? throw new HrValidationException(nameof(id), "Leave request reference does not exist in the current tenant.");
    private async Task RequireMembershipOwnerAsync(string userId, CancellationToken ct) { if (string.IsNullOrWhiteSpace(userId) || !await organizationDb.UserMemberships.AnyAsync(x => x.TenantId == TenantId && x.UserId == userId, ct)) throw new HrValidationException(nameof(userId), "Owner or actor does not belong to the current tenant."); }
    private async Task RequireOrganizationAsync<T>(string id, string parameter, CancellationToken ct) where T : TenantOwnedRecord => _ = await organization.GetAsync<T>(id, ct) ?? throw new HrValidationException(parameter, "Organization reference does not exist in the current tenant.");
    private void Ensure(string? tenant) { if (string.IsNullOrWhiteSpace(tenant) || !string.Equals(tenant, TenantId, StringComparison.Ordinal)) throw new TenantContextValidationException("tenantId", "Record tenant does not match the current tenant context."); }
    private static string Required(string? value) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException("id", "Identifier is required.") : value.Trim();

    // Performance management implementation
    public async Task<PerformanceReview> CreatePerformanceReviewAsync(PerformanceReview review, CancellationToken ct = default)
    {
        Ensure(review.TenantId);
        await RequireEmployeeAsync(review.EmployeeId, ct);
        db.PerformanceReviews.Add(review);
        await db.SaveChangesAsync(ct);
        return review;
    }

    public Task<PerformanceReview?> GetPerformanceReviewAsync(string id, CancellationToken ct = default) =>
        db.PerformanceReviews.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<PerformanceReview>> GetPerformanceReviewsByEmployeeAsync(string employeeId, CancellationToken ct = default) =>
        await db.PerformanceReviews.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<PerformanceGoal> CreatePerformanceGoalAsync(PerformanceGoal goal, CancellationToken ct = default)
    {
        Ensure(goal.TenantId);
        await RequireEmployeeAsync(goal.EmployeeId, ct);
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == goal.PerformanceReviewId && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(goal.PerformanceReviewId), "Performance review does not exist in the current tenant.");
        db.PerformanceGoals.Add(goal);
        await db.SaveChangesAsync(ct);
        return goal;
    }

    public async Task<IReadOnlyList<PerformanceGoal>> GetPerformanceGoalsByReviewAsync(string reviewId, CancellationToken ct = default) =>
        await db.PerformanceGoals.AsNoTracking().Where(x => x.PerformanceReviewId == Required(reviewId) && x.TenantId == TenantId).ToListAsync(ct);

    public async Task<PerformanceFeedback> CreatePerformanceFeedbackAsync(PerformanceFeedback feedback, CancellationToken ct = default)
    {
        Ensure(feedback.TenantId);
        await RequireEmployeeAsync(feedback.FromEmployeeId, ct);
        await RequireEmployeeAsync(feedback.ToEmployeeId, ct);
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == feedback.PerformanceReviewId && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(feedback.PerformanceReviewId), "Performance review does not exist in the current tenant.");
        db.PerformanceFeedbacks.Add(feedback);
        await db.SaveChangesAsync(ct);
        return feedback;
    }

    public async Task<IReadOnlyList<PerformanceFeedback>> GetPerformanceFeedbacksByReviewAsync(string reviewId, CancellationToken ct = default) =>
        await db.PerformanceFeedbacks.AsNoTracking().Where(x => x.PerformanceReviewId == Required(reviewId) && x.TenantId == TenantId).ToListAsync(ct);

    public async Task<CompetencyAssessment> CreateCompetencyAssessmentAsync(CompetencyAssessment assessment, CancellationToken ct = default)
    {
        Ensure(assessment.TenantId);
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == assessment.PerformanceReviewId && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(assessment.PerformanceReviewId), "Performance review does not exist in the current tenant.");
        db.CompetencyAssessments.Add(assessment);
        await db.SaveChangesAsync(ct);
        return assessment;
    }

    public async Task<IReadOnlyList<CompetencyAssessment>> GetCompetencyAssessmentsByReviewAsync(string reviewId, CancellationToken ct = default) =>
        await db.CompetencyAssessments.AsNoTracking().Where(x => x.PerformanceReviewId == Required(reviewId) && x.TenantId == TenantId).ToListAsync(ct);

    public async Task<ReviewCycle> CreateReviewCycleAsync(ReviewCycle cycle, CancellationToken ct = default)
    {
        Ensure(cycle.TenantId);
        db.ReviewCycles.Add(cycle);
        await db.SaveChangesAsync(ct);
        return cycle;
    }

    public async Task<IReadOnlyList<ReviewCycle>> GetActiveReviewCyclesAsync(string tenantId, CancellationToken ct = default) =>
        await db.ReviewCycles.AsNoTracking().Where(x => x.TenantId == Required(tenantId) && x.IsActive).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<AppraisalCommittee> CreateAppraisalCommitteeAsync(AppraisalCommittee committee, CancellationToken ct = default)
    {
        Ensure(committee.TenantId);
        foreach (var memberId in committee.MemberEmployeeIds)
        {
            await RequireEmployeeAsync(memberId, ct);
        }
        db.AppraisalCommittees.Add(committee);
        await db.SaveChangesAsync(ct);
        return committee;
    }

    // Recruitment implementation
    public async Task<JobPosting> CreateJobPostingAsync(JobPosting posting, CancellationToken ct = default)
    {
        Ensure(posting.TenantId);
        await RequireOrganizationAsync<Department>(posting.DepartmentId, nameof(posting.DepartmentId), ct);
        await RequireOrganizationAsync<Location>(posting.LocationId, nameof(posting.LocationId), ct);
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync(ct);
        return posting;
    }

    public Task<JobPosting?> GetJobPostingAsync(string id, CancellationToken ct = default) =>
        db.JobPostings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<JobPosting>> GetJobPostingsByStatusAsync(string tenantId, JobStatus status, CancellationToken ct = default) =>
        await db.JobPostings.AsNoTracking().Where(x => x.TenantId == Required(tenantId) && x.Status == status).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<JobApplication> CreateJobApplicationAsync(JobApplication application, CancellationToken ct = default)
    {
        Ensure(application.TenantId);
        var jobPosting = await db.JobPostings.SingleOrDefaultAsync(x => x.Id == application.JobPostingId && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(application.JobPostingId), "Job posting does not exist in the current tenant.");
        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public Task<JobApplication?> GetJobApplicationAsync(string id, CancellationToken ct = default) =>
        db.JobApplications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<JobApplication>> GetJobApplicationsByPostingAsync(string jobPostingId, CancellationToken ct = default) =>
        await db.JobApplications.AsNoTracking().Where(x => x.JobPostingId == Required(jobPostingId) && x.TenantId == TenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<Interview> CreateInterviewAsync(Interview interview, CancellationToken ct = default)
    {
        Ensure(interview.TenantId);
        await RequireEmployeeAsync(interview.InterviewerEmployeeId, ct);
        var application = await db.JobApplications.SingleOrDefaultAsync(x => x.Id == interview.JobApplicationId && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(interview.JobApplicationId), "Job application does not exist in the current tenant.");
        db.Interviews.Add(interview);
        await db.SaveChangesAsync(ct);
        return interview;
    }

    public async Task<IReadOnlyList<Interview>> GetInterviewsByApplicationAsync(string jobApplicationId, CancellationToken ct = default) =>
        await db.Interviews.AsNoTracking().Where(x => x.JobApplicationId == Required(jobApplicationId) && x.TenantId == TenantId).OrderBy(x => x.ScheduledDateTime).ToListAsync(ct);

    public async Task<JobOffer> CreateJobOfferAsync(JobOffer offer, CancellationToken ct = default)
    {
        Ensure(offer.TenantId);
        var application = await db.JobApplications.SingleOrDefaultAsync(x => x.Id == offer.JobApplicationId && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(offer.JobApplicationId), "Job application does not exist in the current tenant.");
        db.JobOffers.Add(offer);
        await db.SaveChangesAsync(ct);
        return offer;
    }

    public Task<JobOffer?> GetJobOfferAsync(string id, CancellationToken ct = default) =>
        db.JobOffers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    // Disciplinary management implementation
    public async Task<DisciplinaryAction> CreateDisciplinaryActionAsync(DisciplinaryAction action, CancellationToken ct = default)
    {
        Ensure(action.TenantId);
        await RequireEmployeeAsync(action.EmployeeId, ct);
        db.DisciplinaryActions.Add(action);
        await db.SaveChangesAsync(ct);
        return action;
    }

    public Task<DisciplinaryAction?> GetDisciplinaryActionAsync(string id, CancellationToken ct = default) =>
        db.DisciplinaryActions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<DisciplinaryAction>> GetDisciplinaryActionsByEmployeeAsync(string employeeId, CancellationToken ct = default) =>
        await db.DisciplinaryActions.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderByDescending(x => x.EffectiveDate).ToListAsync(ct);

    public async Task<DisciplinaryAction> ResolveDisciplinaryActionAsync(string actionId, string resolvedBy, string? resolutionNotes, CancellationToken ct = default)
    {
        var action = await db.DisciplinaryActions.SingleOrDefaultAsync(x => x.Id == Required(actionId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(actionId), "Disciplinary action does not exist in the current tenant.");
        action.Resolve(resolvedBy, resolutionNotes);
        await db.SaveChangesAsync(ct);
        return action;
    }

    public async Task<DisciplinaryAction> ExpungeDisciplinaryActionAsync(string actionId, CancellationToken ct = default)
    {
        var action = await db.DisciplinaryActions.SingleOrDefaultAsync(x => x.Id == Required(actionId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(actionId), "Disciplinary action does not exist in the current tenant.");
        action.Expunge();
        await db.SaveChangesAsync(ct);
        return action;
    }

    public async Task<DisciplinaryAction> CancelDisciplinaryActionAsync(string actionId, CancellationToken ct = default)
    {
        var action = await db.DisciplinaryActions.SingleOrDefaultAsync(x => x.Id == Required(actionId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(actionId), "Disciplinary action does not exist in the current tenant.");
        action.Cancel();
        await db.SaveChangesAsync(ct);
        return action;
    }

    // Recruitment lifecycle implementation
    public async Task<JobPosting> PublishJobPostingAsync(string jobPostingId, CancellationToken ct = default)
    {
        var posting = await db.JobPostings.SingleOrDefaultAsync(x => x.Id == Required(jobPostingId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(jobPostingId), "Job posting does not exist in the current tenant.");
        posting.Publish();
        await db.SaveChangesAsync(ct);
        return posting;
    }

    public async Task<JobPosting> CloseJobPostingAsync(string jobPostingId, CancellationToken ct = default)
    {
        var posting = await db.JobPostings.SingleOrDefaultAsync(x => x.Id == Required(jobPostingId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(jobPostingId), "Job posting does not exist in the current tenant.");
        posting.Close();
        await db.SaveChangesAsync(ct);
        return posting;
    }

    public async Task<JobApplication> UpdateJobApplicationStatusAsync(string applicationId, ApplicationStatus status, CancellationToken ct = default)
    {
        var application = await db.JobApplications.SingleOrDefaultAsync(x => x.Id == Required(applicationId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(applicationId), "Job application does not exist in the current tenant.");
        application.UpdateStatus(status);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task<Interview> CompleteInterviewAsync(string interviewId, string? notes, CancellationToken ct = default)
    {
        var interview = await db.Interviews.SingleOrDefaultAsync(x => x.Id == Required(interviewId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(interviewId), "Interview does not exist in the current tenant.");
        interview.Complete(notes);
        await db.SaveChangesAsync(ct);
        return interview;
    }

    public async Task<Interview> CancelInterviewAsync(string interviewId, CancellationToken ct = default)
    {
        var interview = await db.Interviews.SingleOrDefaultAsync(x => x.Id == Required(interviewId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(interviewId), "Interview does not exist in the current tenant.");
        interview.Cancel();
        await db.SaveChangesAsync(ct);
        return interview;
    }

    public async Task<JobOffer> SendJobOfferAsync(string offerId, CancellationToken ct = default)
    {
        var offer = await db.JobOffers.SingleOrDefaultAsync(x => x.Id == Required(offerId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(offerId), "Job offer does not exist in the current tenant.");
        offer.Send();
        await db.SaveChangesAsync(ct);
        return offer;
    }

    public async Task<JobOffer> AcceptJobOfferAsync(string offerId, CancellationToken ct = default)
    {
        var offer = await db.JobOffers.SingleOrDefaultAsync(x => x.Id == Required(offerId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(offerId), "Job offer does not exist in the current tenant.");
        offer.Accept();
        await db.SaveChangesAsync(ct);
        return offer;
    }

    public async Task<JobOffer> RejectJobOfferAsync(string offerId, CancellationToken ct = default)
    {
        var offer = await db.JobOffers.SingleOrDefaultAsync(x => x.Id == Required(offerId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(offerId), "Job offer does not exist in the current tenant.");
        offer.Reject();
        await db.SaveChangesAsync(ct);
        return offer;
    }

    public async Task<JobOffer> WithdrawJobOfferAsync(string offerId, CancellationToken ct = default)
    {
        var offer = await db.JobOffers.SingleOrDefaultAsync(x => x.Id == Required(offerId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(offerId), "Job offer does not exist in the current tenant.");
        offer.Withdraw();
        await db.SaveChangesAsync(ct);
        return offer;
    }

    // Performance lifecycle implementation
    public async Task<PerformanceReview> SubmitPerformanceReviewAsync(string reviewId, CancellationToken ct = default)
    {
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == Required(reviewId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(reviewId), "Performance review does not exist in the current tenant.");
        review.Submit();
        await db.SaveChangesAsync(ct);
        return review;
    }

    public async Task<PerformanceReview> StartPerformanceReviewAsync(string reviewId, CancellationToken ct = default)
    {
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == Required(reviewId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(reviewId), "Performance review does not exist in the current tenant.");
        review.StartReview();
        await db.SaveChangesAsync(ct);
        return review;
    }

    public async Task<PerformanceReview> CompletePerformanceReviewAsync(string reviewId, decimal overallScore, string? managerComments, CancellationToken ct = default)
    {
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == Required(reviewId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(reviewId), "Performance review does not exist in the current tenant.");
        review.Complete(overallScore, managerComments);
        await db.SaveChangesAsync(ct);
        return review;
    }

    public async Task<PerformanceReview> AddEmployeeCommentsToReviewAsync(string reviewId, string comments, CancellationToken ct = default)
    {
        var review = await db.PerformanceReviews.SingleOrDefaultAsync(x => x.Id == Required(reviewId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(reviewId), "Performance review does not exist in the current tenant.");
        review.AddEmployeeComments(comments);
        await db.SaveChangesAsync(ct);
        return review;
    }

    public async Task<PerformanceGoal> UpdateGoalProgressAsync(string goalId, decimal actualValue, GoalStatus status, CancellationToken ct = default)
    {
        var goal = await db.PerformanceGoals.SingleOrDefaultAsync(x => x.Id == Required(goalId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(goalId), "Performance goal does not exist in the current tenant.");
        goal.UpdateProgress(actualValue, status);
        await db.SaveChangesAsync(ct);
        return goal;
    }

    public Task<PerformanceGoal?> GetPerformanceGoalAsync(string goalId, CancellationToken ct = default) =>
        db.PerformanceGoals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(goalId) && x.TenantId == TenantId, ct);

    public async Task<ReviewCycle> UpdateReviewCycleAsync(string cycleId, bool isActive, CancellationToken ct = default)
    {
        var cycle = await db.ReviewCycles.SingleOrDefaultAsync(x => x.Id == Required(cycleId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(cycleId), "Review cycle does not exist in the current tenant.");
        if (isActive == cycle.IsActive) return cycle;
        if (isActive) throw new HrValidationException(nameof(isActive), "Cannot reactivate a closed review cycle through this endpoint.");
        cycle.Close();
        await db.SaveChangesAsync(ct);
        return cycle;
    }

    // Checklist lifecycle implementation
    public Task<HrChecklistItem?> GetChecklistItemAsync(string id, CancellationToken ct = default) =>
        db.ChecklistItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<HrChecklistItem> CompleteChecklistItemAsync(string itemId, string evidence, CancellationToken ct = default)
    {
        var item = await db.ChecklistItems.SingleOrDefaultAsync(x => x.Id == Required(itemId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(itemId), "Checklist item does not exist in the current tenant.");
        item.Complete(evidence);
        await db.SaveChangesAsync(ct);
        return item;
    }

    // Payroll enhancement implementation
    public async Task<HrPayrollRecord> CreatePayrollRecordAsync(HrPayrollRecord record, CancellationToken ct = default)
    {
        Ensure(record.TenantId);
        await RequireEmployeeAsync(record.EmployeeId, ct);
        db.HrPayrollRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    public Task<HrPayrollRecord?> GetPayrollRecordAsync(string id, CancellationToken ct = default) =>
        db.HrPayrollRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<HrPayrollRecord>> GetPayrollRecordsByEmployeeAsync(string employeeId, CancellationToken ct = default) =>
        await db.HrPayrollRecords.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderByDescending(x => x.PayPeriodStart).ToListAsync(ct);

    public async Task<HrPayrollRecord> ProcessPayrollRecordAsync(string recordId, CancellationToken ct = default)
    {
        var record = await db.HrPayrollRecords.SingleOrDefaultAsync(x => x.Id == Required(recordId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(recordId), "Payroll record does not exist in the current tenant.");
        record.Process();
        await db.SaveChangesAsync(ct);
        return record;
    }

    // Employee loan methods
    public async Task<EmployeeLoan> CreateEmployeeLoanAsync(EmployeeLoan loan, CancellationToken ct = default)
    {
        Ensure(loan.TenantId);
        await RequireEmployeeAsync(loan.EmployeeId, ct);
        db.EmployeeLoans.Add(loan);
        await db.SaveChangesAsync(ct);
        return loan;
    }

    public Task<EmployeeLoan?> GetEmployeeLoanAsync(string id, CancellationToken ct = default) =>
        db.EmployeeLoans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<EmployeeLoan>> GetEmployeeLoansAsync(string employeeId, CancellationToken ct = default) =>
        await db.EmployeeLoans.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<EmployeeLoan> RepayEmployeeLoanAsync(string loanId, decimal amount, CancellationToken ct = default)
    {
        var loan = await db.EmployeeLoans.SingleOrDefaultAsync(x => x.Id == Required(loanId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(loanId), "Employee loan does not exist in the current tenant.");
        loan.Repay(amount);
        await db.SaveChangesAsync(ct);
        return loan;
    }

    // Payroll advance methods
    public async Task<PayrollAdvance> CreatePayrollAdvanceAsync(PayrollAdvance advance, CancellationToken ct = default)
    {
        Ensure(advance.TenantId);
        await RequireEmployeeAsync(advance.EmployeeId, ct);
        db.PayrollAdvances.Add(advance);
        await db.SaveChangesAsync(ct);
        return advance;
    }

    public Task<PayrollAdvance?> GetPayrollAdvanceAsync(string id, CancellationToken ct = default) =>
        db.PayrollAdvances.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<PayrollAdvance>> GetPayrollAdvancesByEmployeeAsync(string employeeId, CancellationToken ct = default) =>
        await db.PayrollAdvances.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<PayrollAdvance> ApprovePayrollAdvanceAsync(string advanceId, string approvedBy, CancellationToken ct = default)
    {
        var advance = await db.PayrollAdvances.SingleOrDefaultAsync(x => x.Id == Required(advanceId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(advanceId), "Payroll advance does not exist in the current tenant.");
        advance.Approve(approvedBy);
        await db.SaveChangesAsync(ct);
        return advance;
    }

    // Advanced time and attendance methods
    public async Task<TimeEntry> CreateTimeEntryAsync(TimeEntry entry, CancellationToken ct = default)
    {
        Ensure(entry.TenantId);
        await RequireEmployeeAsync(entry.EmployeeId, ct);
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public Task<TimeEntry?> GetTimeEntryAsync(string id, CancellationToken ct = default) =>
        db.TimeEntries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<TimeEntry>> GetTimeEntriesByEmployeeAsync(string employeeId, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default)
    {
        var query = db.TimeEntries.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId);
        if (startDate.HasValue) query = query.Where(x => x.ClockIn.Date >= startDate.Value.ToDateTime(TimeOnly.MinValue));
        if (endDate.HasValue) query = query.Where(x => x.ClockIn.Date <= endDate.Value.ToDateTime(TimeOnly.MaxValue));
        return await query.OrderByDescending(x => x.ClockIn).ToListAsync(ct);
    }

    public async Task<TimeEntry> ClockOutTimeEntryAsync(string entryId, DateTimeOffset clockOutTime, string? notes = null, CancellationToken ct = default)
    {
        var entry = await db.TimeEntries.SingleOrDefaultAsync(x => x.Id == Required(entryId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(entryId), "Time entry does not exist in the current tenant.");
        entry.RecordClockOut(clockOutTime, notes);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    // Work schedule methods
    public async Task<WorkSchedule> CreateWorkScheduleAsync(WorkSchedule schedule, CancellationToken ct = default)
    {
        Ensure(schedule.TenantId);
        await RequireEmployeeAsync(schedule.EmployeeId, ct);
        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<IReadOnlyList<WorkSchedule>> GetWorkSchedulesByEmployeeAsync(string employeeId, CancellationToken ct = default) =>
        await db.WorkSchedules.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId && x.IsActive).OrderBy(x => x.DayOfWeek).ToListAsync(ct);

    // Overtime request methods
    public async Task<OvertimeRequest> CreateOvertimeRequestAsync(OvertimeRequest request, CancellationToken ct = default)
    {
        Ensure(request.TenantId);
        await RequireEmployeeAsync(request.EmployeeId, ct);
        db.OvertimeRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }

    public Task<OvertimeRequest?> GetOvertimeRequestAsync(string id, CancellationToken ct = default) =>
        db.OvertimeRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Required(id) && x.TenantId == TenantId, ct);

    public async Task<IReadOnlyList<OvertimeRequest>> GetOvertimeRequestsByEmployeeAsync(string employeeId, CancellationToken ct = default) =>
        await db.OvertimeRequests.AsNoTracking().Where(x => x.EmployeeId == Required(employeeId) && x.TenantId == TenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<OvertimeRequest> ApproveOvertimeRequestAsync(string requestId, string approvedBy, CancellationToken ct = default)
    {
        var request = await db.OvertimeRequests.SingleOrDefaultAsync(x => x.Id == Required(requestId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(requestId), "Overtime request does not exist in the current tenant.");
        await RequireMembershipOwnerAsync(approvedBy, ct);
        request.Approve(approvedBy);
        await db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<OvertimeRequest> RejectOvertimeRequestAsync(string requestId, CancellationToken ct = default)
    {
        var request = await db.OvertimeRequests.SingleOrDefaultAsync(x => x.Id == Required(requestId) && x.TenantId == TenantId, ct)
            ?? throw new HrValidationException(nameof(requestId), "Overtime request does not exist in the current tenant.");
        request.Reject();
        await db.SaveChangesAsync(ct);
        return request;
    }
}

public static class HrPersistence
{
    public static async Task InitializeAsync(HrDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await HasEmployeeTableAsync(db, cancellationToken))
            return;

        await db.Database.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(cancellationToken);
    }

    private static async Task<bool> HasEmployeeTableAsync(HrDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var table = db.Model.FindEntityType(typeof(Employee))?.GetTableName()
            ?? throw new InvalidOperationException("The HR employee table is not configured.");
        var schema = db.Model.FindEntityType(typeof(Employee))?.GetSchema();
        await using var command = connection.CreateCommand();

        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $table);";
            AddParameter(command, "$table", table);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        }

        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = @table AND table_schema = COALESCE(@schema, current_schema()));";
            AddParameter(command, "@table", table);
            AddParameter(command, "@schema", (object?)schema ?? DBNull.Value);
            return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
        }

        throw new NotSupportedException($"HR schema inspection is not supported for provider '{db.Database.ProviderName}'.");
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

public interface IEmployeeDocumentStorage { Task<string> RegisterAsync(string tenantId, string fileReference, CancellationToken ct = default); }
public sealed class ExternalDocumentStorageBoundary : IEmployeeDocumentStorage
{ public Task<string> RegisterAsync(string tenantId, string fileReference, CancellationToken ct = default) { if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(fileReference)) throw new HrValidationException(nameof(fileReference), "A tenant and external file reference are required."); return Task.FromResult(fileReference.Trim()); } }

public sealed record AttendanceImportRow(int RowNumber, string EmployeeNumber, DateOnly Date, TimeOnly? ClockIn, TimeOnly? ClockOut);
public sealed record AttendanceImportError(int RowNumber, string Code, string Message);
public sealed record AttendanceImportResult(IReadOnlyList<AttendanceImportRow> Rows, IReadOnlyList<AttendanceImportError> Errors);
public interface IAttendanceImportValidator { AttendanceImportResult Validate(IEnumerable<string[]> rows); }
public sealed class AttendanceImportValidator : IAttendanceImportValidator
{
    public AttendanceImportResult Validate(IEnumerable<string[]> rows)
    { ArgumentNullException.ThrowIfNull(rows); var valid = new List<AttendanceImportRow>(); var errors = new List<AttendanceImportError>(); var rowNumber = 1; foreach (var row in rows) { if (row is null || row.Length < 3) { errors.Add(new(rowNumber, "INVALID_COLUMNS", "Expected employee number, date, clock-in and optional clock-out.")); rowNumber++; continue; } if (string.IsNullOrWhiteSpace(row[0]) || !DateOnly.TryParse(row[1], out var date) || !TimeOnly.TryParse(row[2], out var clockIn) || (row.Length > 3 && !string.IsNullOrWhiteSpace(row[3]) && !TimeOnly.TryParse(row[3], out _))) { errors.Add(new(rowNumber, "INVALID_VALUE", "Attendance values must be explicitly valid; no normalization was applied.")); rowNumber++; continue; } TimeOnly? clockOut = row.Length > 3 && !string.IsNullOrWhiteSpace(row[3]) ? TimeOnly.Parse(row[3]) : null; if (clockOut is not null && clockOut < clockIn) errors.Add(new(rowNumber, "INVALID_RANGE", "Clock-out cannot precede clock-in.")); else valid.Add(new(rowNumber, row[0].Trim(), date, clockIn, clockOut)); rowNumber++; } return new(valid, errors); }
}