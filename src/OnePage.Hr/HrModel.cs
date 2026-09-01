using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Hr;

public sealed class HrValidationException(string parameterName, string message) : ArgumentException(message, parameterName);
public enum EmploymentStatus { Preboarding, Active, OnLeave, Offboarding, Terminated }
public enum ChecklistKind { Onboarding, Offboarding }
public enum ChecklistStatus { Pending, InProgress, Complete, Cancelled }
public enum LeaveRequestStatus { Pending, Approved, Rejected, Cancelled }
public enum PerformanceReviewStatus { Draft, Submitted, InReview, Completed, Cancelled }
public enum GoalStatus { NotStarted, InProgress, Completed, Overdue, Cancelled }
public enum FeedbackType { Peer, Manager, Self, Subordinate }
public enum ReviewFramework { OKR, BalancedScorecard, Custom }

// Recruitment enums
public enum JobStatus { Draft, Published, Closed, Cancelled }
public enum ApplicationStatus { Applied, Screening, Interview, Offer, Accepted, Rejected, Withdrawn }
public enum InterviewStatus { Scheduled, Completed, Cancelled, NoShow }
public enum OfferStatus { Draft, Sent, Accepted, Rejected, Withdrawn }

// Disciplinary management enums
public enum DisciplinaryActionType { Warning, Query, Suspension, Termination }
public enum DisciplinarySeverity { Low, Medium, High, Critical }
public enum DisciplinaryStatus { Active, Resolved, Expunged, Cancelled }

public sealed class Employee
{
    private Employee() { }
    public Employee(string id, string tenantId, string employeeNumber, string firstName, string lastName, string? email = null, string? phone = null, string? governmentId = null)
    {
        Id = Required(id, nameof(id), "Employee ID is required."); TenantId = Required(tenantId, nameof(tenantId), "Tenant ID is required.");
        EmployeeNumber = Required(employeeNumber, nameof(employeeNumber), "Employee number is required."); FirstName = Required(firstName, nameof(firstName), "First name is required."); LastName = Required(lastName, nameof(lastName), "Last name is required.");
        Email = Optional(email); Phone = Optional(phone); GovernmentId = Optional(governmentId); CreatedAt = DateTimeOffset.UtcNow; IsActive = true;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeNumber { get; private set; } = null!;
    public string FirstName { get; private set; } = null!; public string LastName { get; private set; } = null!; public string? Email { get; private set; } public string? Phone { get; private set; }
    public string? GovernmentId { get; private set; } public bool IsActive { get; private set; } public DateOnly? TerminationDate { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    public void UpdateContact(string? email, string? phone) { Email = Optional(email); Phone = Optional(phone); }
    public void Offboard(DateOnly effectiveDate) { if (effectiveDate == default) throw new HrValidationException(nameof(effectiveDate), "Termination date is required."); IsActive = false; TerminationDate = effectiveDate; }
    private static string Required(string? value, string name, string message) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, message) : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class Employment
{
    private Employment() { }
    public Employment(string id, string tenantId, string employeeId, DateOnly effectiveFrom, string legalEntityId, string departmentId, string position, string? managerEmployeeId, string locationId, EmploymentStatus status = EmploymentStatus.Active, DateOnly? effectiveTo = null)
    {
        Id = Required(id, nameof(id), "Employment ID is required."); TenantId = Required(tenantId, nameof(tenantId), "Tenant ID is required."); EmployeeId = Required(employeeId, nameof(employeeId), "Employee ID is required.");
        LegalEntityId = Required(legalEntityId, nameof(legalEntityId), "Legal entity is required."); DepartmentId = Required(departmentId, nameof(departmentId), "Department is required."); Position = Required(position, nameof(position), "Position is required."); LocationId = Required(locationId, nameof(locationId), "Location is required.");
        if (effectiveTo < effectiveFrom) throw new HrValidationException(nameof(effectiveTo), "Effective end cannot precede effective start.");
        EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo; ManagerEmployeeId = Optional(managerEmployeeId); Status = status;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!; public DateOnly EffectiveFrom { get; private set; } public DateOnly? EffectiveTo { get; private set; }
    public string LegalEntityId { get; private set; } = null!; public string DepartmentId { get; private set; } = null!; public string Position { get; private set; } = null!; public string? ManagerEmployeeId { get; private set; } public string LocationId { get; private set; } = null!; public EmploymentStatus Status { get; private set; }
    public void ChangeStatus(EmploymentStatus status) => Status = status;
    private static string Required(string? value, string name, string message) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, message) : value.Trim();
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HrChecklistItem
{
    private HrChecklistItem() { }
    public HrChecklistItem(string id, string tenantId, string employeeId, ChecklistKind kind, string title, string ownerUserId, DateOnly? dueDate = null)
    { Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId)); Title = Required(title, nameof(title)); OwnerUserId = Required(ownerUserId, nameof(ownerUserId)); Kind = kind; DueDate = dueDate; Status = ChecklistStatus.Pending; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!; public ChecklistKind Kind { get; private set; } public string Title { get; private set; } = null!; public string OwnerUserId { get; private set; } = null!; public DateOnly? DueDate { get; private set; } public ChecklistStatus Status { get; private set; } public string? CompletionEvidence { get; private set; } public DateTimeOffset? CompletedAt { get; private set; }
    public void Complete(string evidence) { CompletionEvidence = Required(evidence, nameof(evidence)); Status = ChecklistStatus.Complete; CompletedAt = DateTimeOffset.UtcNow; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class LeavePolicy
{
    private LeavePolicy() { }
    public LeavePolicy(string id, string tenantId, string code, string name, decimal annualEntitlement, bool allowCarryover = false)
    { if (annualEntitlement < 0) throw new HrValidationException(nameof(annualEntitlement), "Annual entitlement cannot be negative."); Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); Code = Required(code, nameof(code)); Name = Required(name, nameof(name)); AnnualEntitlement = annualEntitlement; AllowCarryover = allowCarryover; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string Code { get; private set; } = null!; public string Name { get; private set; } = null!; public decimal AnnualEntitlement { get; private set; } public bool AllowCarryover { get; private set; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class LeaveBalance
{
    private LeaveBalance() { }
    public LeaveBalance(string id, string tenantId, string employeeId, string policyId, int year, decimal entitledDays) { if (year < 1 || year > 9999) throw new HrValidationException(nameof(year), "Leave year is invalid."); if (entitledDays < 0) throw new HrValidationException(nameof(entitledDays), "Entitlement cannot be negative."); Id = Required(id); TenantId = Required(tenantId); EmployeeId = Required(employeeId); PolicyId = Required(policyId); Year = year; EntitledDays = entitledDays; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!; public string PolicyId { get; private set; } = null!; public int Year { get; private set; } public decimal EntitledDays { get; private set; } public decimal UsedDays { get; private set; } public decimal AvailableDays => EntitledDays - UsedDays;
    public void Reserve(decimal days) { if (days <= 0) throw new HrValidationException(nameof(days), "Leave days must be positive."); if (days > AvailableDays) throw new LeaveBalanceException("Insufficient leave balance."); UsedDays += days; }
    private static string Required(string? value) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException("id", "Identifier is required.") : value.Trim();
}
public sealed class LeaveBalanceException(string message) : InvalidOperationException(message);

public sealed class LeaveRequest
{
    private LeaveRequest() { }
    public LeaveRequest(string id, string tenantId, string employeeId, string policyId, DateOnly startDate, DateOnly endDate, decimal days, string reason)
    { if (endDate < startDate) throw new HrValidationException(nameof(endDate), "Leave end cannot precede start."); if (days <= 0) throw new HrValidationException(nameof(days), "Leave days must be positive."); Id = Required(id); TenantId = Required(tenantId); EmployeeId = Required(employeeId); PolicyId = Required(policyId); StartDate = startDate; EndDate = endDate; Days = days; Reason = Required(reason); Status = LeaveRequestStatus.Pending; CreatedAt = DateTimeOffset.UtcNow; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!; public string PolicyId { get; private set; } = null!; public DateOnly StartDate { get; private set; } public DateOnly EndDate { get; private set; } public decimal Days { get; private set; } public string Reason { get; private set; } = null!; public LeaveRequestStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    public void Approve() { if (Status != LeaveRequestStatus.Pending) throw new HrValidationException(nameof(Status), "Only pending leave can be approved."); Status = LeaveRequestStatus.Approved; }
    public void Reject() { if (Status != LeaveRequestStatus.Pending) throw new HrValidationException(nameof(Status), "Only pending leave can be rejected."); Status = LeaveRequestStatus.Rejected; }
    private static string Required(string? value) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException("id", "Identifier is required.") : value.Trim();
}
public sealed class LeaveDecision
{
    private LeaveDecision() { }
    public LeaveDecision(string id, string tenantId, string leaveRequestId, string actorUserId, string decision, string? comment) { Id = Required(id); TenantId = Required(tenantId); LeaveRequestId = Required(leaveRequestId); ActorUserId = Required(actorUserId); Decision = Required(decision); Comment = comment; DecidedAt = DateTimeOffset.UtcNow; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string LeaveRequestId { get; private set; } = null!; public string ActorUserId { get; private set; } = null!; public string Decision { get; private set; } = null!; public string? Comment { get; private set; } public DateTimeOffset DecidedAt { get; private set; }
    private static string Required(string? value) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException("id", "Identifier is required.") : value.Trim();
}

public sealed class HrAccessReviewRequest
{
    private HrAccessReviewRequest() { }
    public HrAccessReviewRequest(string id, string tenantId, string employeeId, string requestedByUserId, DateOnly effectiveDate)
    { Id = Required(id); TenantId = Required(tenantId); EmployeeId = Required(employeeId); RequestedByUserId = Required(requestedByUserId); EffectiveDate = effectiveDate == default ? throw new HrValidationException(nameof(effectiveDate), "Effective date is required.") : effectiveDate; RequestedAt = DateTimeOffset.UtcNow; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!; public string RequestedByUserId { get; private set; } = null!; public DateOnly EffectiveDate { get; private set; } public DateTimeOffset RequestedAt { get; private set; }
    private static string Required(string? value) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException("value", "Value is required.") : value.Trim();
}

public sealed class EmployeeDocument
{
    private EmployeeDocument() { }
    public EmployeeDocument(string id, string tenantId, string employeeId, string documentType, string fileReference, DateOnly? expiresOn = null) { Id = Required(id); TenantId = Required(tenantId); EmployeeId = Required(employeeId); DocumentType = Required(documentType); FileReference = Required(fileReference); ExpiresOn = expiresOn; CreatedAt = DateTimeOffset.UtcNow; }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!; public string DocumentType { get; private set; } = null!; public string FileReference { get; private set; } = null!; public DateOnly? ExpiresOn { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    private static string Required(string? value) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException("value", "Document metadata is required.") : value.Trim();
}

public sealed class PerformanceReview
{
    private PerformanceReview() { }
    public PerformanceReview(string id, string tenantId, string employeeId, string reviewCycleId, ReviewFramework framework, DateOnly reviewPeriodStart, DateOnly reviewPeriodEnd)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId)); 
        ReviewCycleId = Required(reviewCycleId, nameof(reviewCycleId)); Framework = framework;
        if (reviewPeriodEnd < reviewPeriodStart) throw new HrValidationException(nameof(reviewPeriodEnd), "Review period end cannot precede start.");
        ReviewPeriodStart = reviewPeriodStart; ReviewPeriodEnd = reviewPeriodEnd;
        Status = PerformanceReviewStatus.Draft; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public string ReviewCycleId { get; private set; } = null!; public ReviewFramework Framework { get; private set; }
    public DateOnly ReviewPeriodStart { get; private set; } public DateOnly ReviewPeriodEnd { get; private set; }
    public decimal? OverallScore { get; private set; } public string? ManagerComments { get; private set; }
    public string? EmployeeComments { get; private set; } public PerformanceReviewStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? SubmittedAt { get; private set; } public DateTimeOffset? CompletedAt { get; private set; }
    
    public void Submit() { if (Status != PerformanceReviewStatus.Draft) throw new HrValidationException(nameof(Status), "Only draft reviews can be submitted."); Status = PerformanceReviewStatus.Submitted; SubmittedAt = DateTimeOffset.UtcNow; }
    public void StartReview() { if (Status != PerformanceReviewStatus.Submitted) throw new HrValidationException(nameof(Status), "Only submitted reviews can be reviewed."); Status = PerformanceReviewStatus.InReview; }
    public void Complete(decimal overallScore, string? managerComments = null) 
    { 
        if (Status != PerformanceReviewStatus.InReview) throw new HrValidationException(nameof(Status), "Only in-review reviews can be completed."); 
        if (overallScore < 0 || overallScore > 100) throw new HrValidationException(nameof(overallScore), "Overall score must be between 0 and 100.");
        OverallScore = overallScore; ManagerComments = managerComments; Status = PerformanceReviewStatus.Completed; CompletedAt = DateTimeOffset.UtcNow; 
    }
    public void AddEmployeeComments(string comments) { EmployeeComments = comments; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class PerformanceGoal
{
    private PerformanceGoal() { }
    public PerformanceGoal(string id, string tenantId, string employeeId, string performanceReviewId, string title, string description, decimal targetValue, DateOnly dueDate)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        PerformanceReviewId = Required(performanceReviewId, nameof(performanceReviewId)); Title = Required(title, nameof(title));
        Description = Required(description, nameof(description)); TargetValue = targetValue; DueDate = dueDate;
        Status = GoalStatus.NotStarted; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public string PerformanceReviewId { get; private set; } = null!; public string Title { get; private set; } = null!; public string Description { get; private set; } = null!;
    public decimal TargetValue { get; private set; } public decimal? ActualValue { get; private set; } public DateOnly DueDate { get; private set; }
    public GoalStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? CompletedAt { get; private set; }
    
    public void UpdateProgress(decimal actualValue, GoalStatus status) 
    { 
        ActualValue = actualValue; Status = status; 
        if (status == GoalStatus.Completed) CompletedAt = DateTimeOffset.UtcNow;
    }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class PerformanceFeedback
{
    private PerformanceFeedback() { }
    public PerformanceFeedback(string id, string tenantId, string performanceReviewId, string fromEmployeeId, string toEmployeeId, FeedbackType feedbackType, string content, bool isAnonymous = false)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); PerformanceReviewId = Required(performanceReviewId, nameof(performanceReviewId));
        FromEmployeeId = Required(fromEmployeeId, nameof(fromEmployeeId)); ToEmployeeId = Required(toEmployeeId, nameof(toEmployeeId));
        FeedbackType = feedbackType; Content = Required(content, nameof(content)); IsAnonymous = isAnonymous;
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string PerformanceReviewId { get; private set; } = null!;
    public string FromEmployeeId { get; private set; } = null!; public string ToEmployeeId { get; private set; } = null!; public FeedbackType FeedbackType { get; private set; }
    public string Content { get; private set; } = null!; public bool IsAnonymous { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class CompetencyAssessment
{
    private CompetencyAssessment() { }
    public CompetencyAssessment(string id, string tenantId, string performanceReviewId, string competencyName, string competencyDescription, decimal score, string? comments = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); PerformanceReviewId = Required(performanceReviewId, nameof(performanceReviewId));
        CompetencyName = Required(competencyName, nameof(competencyName)); CompetencyDescription = Required(competencyDescription, nameof(competencyDescription));
        if (score < 0 || score > 5) throw new HrValidationException(nameof(score), "Competency score must be between 0 and 5.");
        Score = score; Comments = comments; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string PerformanceReviewId { get; private set; } = null!;
    public string CompetencyName { get; private set; } = null!; public string CompetencyDescription { get; private set; } = null!;
    public decimal Score { get; private set; } public string? Comments { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class ReviewCycle
{
    private ReviewCycle() { }
    public ReviewCycle(string id, string tenantId, string name, string description, DateOnly startDate, DateOnly endDate, ReviewFramework framework)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); Name = Required(name, nameof(name));
        Description = Required(description, nameof(description)); Framework = framework;
        if (endDate < startDate) throw new HrValidationException(nameof(endDate), "Review cycle end cannot precede start.");
        StartDate = startDate; EndDate = endDate; IsActive = true; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!; public ReviewFramework Framework { get; private set; }
    public DateOnly StartDate { get; private set; } public DateOnly EndDate { get; private set; } public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? ClosedAt { get; private set; }
    
    public void Close() { IsActive = false; ClosedAt = DateTimeOffset.UtcNow; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class AppraisalCommittee
{
    private AppraisalCommittee() { }
    public AppraisalCommittee(string id, string tenantId, string name, string description, IReadOnlyList<string> memberEmployeeIds)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); Name = Required(name, nameof(name));
        Description = Required(description, nameof(description)); MemberEmployeeIds = memberEmployeeIds ?? throw new HrValidationException(nameof(memberEmployeeIds), "Committee members are required.");
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!; public IReadOnlyList<string> MemberEmployeeIds { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

// Recruitment models
public sealed class JobPosting
{
    private JobPosting() { }
    public JobPosting(string id, string tenantId, string title, string description, string departmentId, string locationId, string? requirements = null, string? responsibilities = null, decimal? minSalary = null, decimal? maxSalary = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); Title = Required(title, nameof(title));
        Description = Required(description, nameof(description)); DepartmentId = Required(departmentId, nameof(departmentId));
        LocationId = Required(locationId, nameof(locationId)); Requirements = requirements; Responsibilities = responsibilities;
        if (minSalary.HasValue && maxSalary.HasValue && minSalary > maxSalary) throw new HrValidationException(nameof(minSalary), "Minimum salary cannot exceed maximum salary.");
        MinSalary = minSalary; MaxSalary = maxSalary; Status = JobStatus.Draft; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!; public string DepartmentId { get; private set; } = null!; public string LocationId { get; private set; } = null!;
    public string? Requirements { get; private set; } public string? Responsibilities { get; private set; } public decimal? MinSalary { get; private set; }
    public decimal? MaxSalary { get; private set; } public JobStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; } public DateTimeOffset? ClosedAt { get; private set; }
    
    public void Publish() { if (Status != JobStatus.Draft) throw new HrValidationException(nameof(Status), "Only draft jobs can be published."); Status = JobStatus.Published; PublishedAt = DateTimeOffset.UtcNow; }
    public void Close() { if (Status != JobStatus.Published) throw new HrValidationException(nameof(Status), "Only published jobs can be closed."); Status = JobStatus.Closed; ClosedAt = DateTimeOffset.UtcNow; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class JobApplication
{
    private JobApplication() { }
    public JobApplication(string id, string tenantId, string jobPostingId, string candidateName, string candidateEmail, string? candidatePhone = null, string? resumeUrl = null, string? coverLetter = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); JobPostingId = Required(jobPostingId, nameof(jobPostingId));
        CandidateName = Required(candidateName, nameof(candidateName)); CandidateEmail = Required(candidateEmail, nameof(candidateEmail));
        CandidatePhone = candidatePhone; ResumeUrl = resumeUrl; CoverLetter = coverLetter;
        Status = ApplicationStatus.Applied; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string JobPostingId { get; private set; } = null!;
    public string CandidateName { get; private set; } = null!; public string CandidateEmail { get; private set; } = null!; public string? CandidatePhone { get; private set; }
    public string? ResumeUrl { get; private set; } public string? CoverLetter { get; private set; } public ApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? UpdatedAt { get; private set; }
    
    public void UpdateStatus(ApplicationStatus status) { Status = status; UpdatedAt = DateTimeOffset.UtcNow; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class Interview
{
    private Interview() { }
    public Interview(string id, string tenantId, string jobApplicationId, string interviewerEmployeeId, DateTimeOffset scheduledDateTime, string? location = null, string? notes = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); JobApplicationId = Required(jobApplicationId, nameof(jobApplicationId));
        InterviewerEmployeeId = Required(interviewerEmployeeId, nameof(interviewerEmployeeId)); ScheduledDateTime = scheduledDateTime;
        Location = location; Notes = notes; Status = InterviewStatus.Scheduled; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string JobApplicationId { get; private set; } = null!;
    public string InterviewerEmployeeId { get; private set; } = null!; public DateTimeOffset ScheduledDateTime { get; private set; }
    public string? Location { get; private set; } public string? Notes { get; private set; } public InterviewStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? CompletedAt { get; private set; }
    
    public void Complete(string? notes = null) { if (Status != InterviewStatus.Scheduled) throw new HrValidationException(nameof(Status), "Only scheduled interviews can be completed."); Status = InterviewStatus.Completed; CompletedAt = DateTimeOffset.UtcNow; Notes = notes ?? Notes; }
    public void Cancel() { Status = InterviewStatus.Cancelled; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class JobOffer
{
    private JobOffer() { }
    public JobOffer(string id, string tenantId, string jobApplicationId, decimal salary, string? benefits = null, DateTimeOffset? startDate = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); JobApplicationId = Required(jobApplicationId, nameof(jobApplicationId));
        if (salary <= 0) throw new HrValidationException(nameof(salary), "Salary must be positive."); Salary = salary; Benefits = benefits;
        StartDate = startDate ?? DateTimeOffset.UtcNow.AddDays(30); Status = OfferStatus.Draft; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string JobApplicationId { get; private set; } = null!;
    public decimal Salary { get; private set; } public string? Benefits { get; private set; } public DateTimeOffset StartDate { get; private set; }
    public OfferStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    
    public void Send() { if (Status != OfferStatus.Draft) throw new HrValidationException(nameof(Status), "Only draft offers can be sent."); Status = OfferStatus.Sent; SentAt = DateTimeOffset.UtcNow; }
    public void Accept() { if (Status != OfferStatus.Sent) throw new HrValidationException(nameof(Status), "Only sent offers can be accepted."); Status = OfferStatus.Accepted; RespondedAt = DateTimeOffset.UtcNow; }
    public void Reject() { if (Status != OfferStatus.Sent && Status != OfferStatus.Draft) throw new HrValidationException(nameof(Status), "Only sent or draft offers can be rejected."); Status = OfferStatus.Rejected; RespondedAt = DateTimeOffset.UtcNow; }
    public void Withdraw() { Status = OfferStatus.Withdrawn; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

// Disciplinary management models
public sealed class DisciplinaryAction
{
    private DisciplinaryAction() { }
    public DisciplinaryAction(string id, string tenantId, string employeeId, DisciplinaryActionType actionType, DisciplinarySeverity severity, string reason, string description, DateOnly effectiveDate, DateOnly? expiryDate = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        ActionType = actionType; Severity = severity; Reason = Required(reason, nameof(reason));
        Description = Required(description, nameof(description)); EffectiveDate = effectiveDate;
        if (expiryDate.HasValue && expiryDate < effectiveDate) throw new HrValidationException(nameof(expiryDate), "Expiry date cannot precede effective date.");
        ExpiryDate = expiryDate; Status = DisciplinaryStatus.Active; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public DisciplinaryActionType ActionType { get; private set; } public DisciplinarySeverity Severity { get; private set; }
    public string Reason { get; private set; } = null!; public string Description { get; private set; } = null!;
    public DateOnly EffectiveDate { get; private set; } public DateOnly? ExpiryDate { get; private set; }
    public DisciplinaryStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; } public string? ResolvedBy { get; private set; }
    public string? ResolutionNotes { get; private set; }
    
    public void Resolve(string resolvedBy, string? resolutionNotes = null)
    {
        if (Status != DisciplinaryStatus.Active) throw new HrValidationException(nameof(Status), "Only active disciplinary actions can be resolved.");
        Status = DisciplinaryStatus.Resolved; ResolvedBy = Required(resolvedBy, nameof(resolvedBy));
        ResolvedAt = DateTimeOffset.UtcNow; ResolutionNotes = resolutionNotes;
    }
    public void Expunge() { Status = DisciplinaryStatus.Expunged; }
    public void Cancel() { Status = DisciplinaryStatus.Cancelled; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

// Payroll enhancement models
public sealed class HrPayrollRecord
{
    private HrPayrollRecord() { }
    public HrPayrollRecord(string id, string tenantId, string employeeId, DateOnly payPeriodStart, DateOnly payPeriodEnd, decimal grossPay, decimal taxDeduction, decimal pensionDeduction, decimal netPay, string currency = "USD")
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        if (payPeriodEnd < payPeriodStart) throw new HrValidationException(nameof(payPeriodEnd), "Pay period end cannot precede start.");
        PayPeriodStart = payPeriodStart; PayPeriodEnd = payPeriodEnd;
        if (grossPay < 0) throw new HrValidationException(nameof(grossPay), "Gross pay cannot be negative.");
        GrossPay = grossPay; TaxDeduction = taxDeduction; PensionDeduction = pensionDeduction; NetPay = netPay;
        Currency = Required(currency, nameof(currency)); Status = HrPayrollStatus.Generated; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public DateOnly PayPeriodStart { get; private set; } public DateOnly PayPeriodEnd { get; private set; }
    public decimal GrossPay { get; private set; } public decimal TaxDeduction { get; private set; } public decimal PensionDeduction { get; private set; }
    public decimal NetPay { get; private set; } public string Currency { get; private set; } = null!;
    public HrPayrollStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? ProcessedAt { get; private set; }
    public string? PayslipUrl { get; private set; }
    
    // Enhanced payroll fields
    public string? TaxCode { get; set; }
    public decimal TaxRate { get; set; }
    public string? PensionScheme { get; set; }
    public decimal PensionRate { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal AdvanceAmount { get; set; }
    public decimal LoanRepaymentAmount { get; set; }
    public string? CountryCode { get; set; }
    
    public void Process() { if (Status != HrPayrollStatus.Generated) throw new HrValidationException(nameof(Status), "Only generated payroll can be processed."); Status = HrPayrollStatus.Processed; ProcessedAt = DateTimeOffset.UtcNow; }
    public void AttachPayslip(string payslipUrl) { PayslipUrl = Required(payslipUrl, nameof(payslipUrl)); }
    public void Pay() { if (Status != HrPayrollStatus.Processed) throw new HrValidationException(nameof(Status), "Only processed payroll can be paid."); Status = HrPayrollStatus.Paid; }
    public void AddBonus(decimal amount) { BonusAmount += amount; RecalculateNetPay(); }
    public void AddDeduction(decimal amount) { DeductionAmount += amount; RecalculateNetPay(); }
    public void SetTaxDetails(string taxCode, decimal rate) { TaxCode = taxCode; TaxRate = rate; RecalculateTax(); }
    public void SetPensionDetails(string scheme, decimal rate) { PensionScheme = scheme; PensionRate = rate; RecalculatePension(); }
    public void AddAdvance(decimal amount) { AdvanceAmount += amount; RecalculateNetPay(); }
    public void AddLoanRepayment(decimal amount) { LoanRepaymentAmount += amount; RecalculateNetPay(); }
    
    private void RecalculateTax() { TaxDeduction = TaxCalculator.CalculateTax(GrossPay, TaxCode, CountryCode ?? "US"); RecalculateNetPay(); }
    private void RecalculatePension() { PensionDeduction = PensionCalculator.CalculateEmployeePension(GrossPay, PensionRate); RecalculateNetPay(); }
    private void RecalculateNetPay() { NetPay = GrossPay - TaxDeduction - PensionDeduction - DeductionAmount - AdvanceAmount - LoanRepaymentAmount + BonusAmount; }
    
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public enum HrPayrollStatus { Generated, Processed, Paid, Cancelled }

// Payroll advance model
public sealed class PayrollAdvance
{
    private PayrollAdvance() { }
    public PayrollAdvance(string id, string tenantId, string employeeId, decimal amount, int repaymentPeriods, decimal interestRate = 0, string? reason = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        if (amount <= 0) throw new HrValidationException(nameof(amount), "Advance amount must be positive.");
        Amount = amount; RepaymentPeriods = repaymentPeriods; InterestRate = interestRate; Reason = reason;
        Status = AdvanceStatus.Pending; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public decimal Amount { get; private set; } public int RepaymentPeriods { get; private set; } public decimal InterestRate { get; private set; }
    public string? Reason { get; private set; } public AdvanceStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; } public string? ApprovedBy { get; private set; }
    public decimal? MonthlyRepayment { get; private set; } public decimal? TotalRepayment { get; private set; }
    
    public void Approve(string approvedBy) { if (Status != AdvanceStatus.Pending) throw new HrValidationException(nameof(Status), "Only pending advances can be approved."); Status = AdvanceStatus.Approved; ApprovedBy = approvedBy; ApprovedAt = DateTimeOffset.UtcNow; CalculateRepayment(); }
    public void Reject() { if (Status != AdvanceStatus.Pending) throw new HrValidationException(nameof(Status), "Only pending advances can be rejected."); Status = AdvanceStatus.Rejected; }
    public void Cancel() { if (Status != AdvanceStatus.Approved) throw new HrValidationException(nameof(Status), "Only approved advances can be cancelled."); Status = AdvanceStatus.Cancelled; }
    
    private void CalculateRepayment() { MonthlyRepayment = PayrollAdvanceCalculator.CalculateAdvanceRepayment(Amount, RepaymentPeriods, InterestRate); TotalRepayment = MonthlyRepayment * RepaymentPeriods; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public enum AdvanceStatus { Pending, Approved, Rejected, Cancelled, FullyRepaid }

public sealed class EmployeeLoan
{
    private EmployeeLoan() { }
    public EmployeeLoan(string id, string tenantId, string employeeId, decimal amount, decimal interestRate, DateOnly startDate, DateOnly endDate, string? description = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        if (amount <= 0) throw new HrValidationException(nameof(amount), "Loan amount must be positive.");
        if (interestRate < 0) throw new HrValidationException(nameof(interestRate), "Interest rate cannot be negative.");
        if (endDate < startDate) throw new HrValidationException(nameof(endDate), "Loan end date cannot precede start date.");
        Amount = amount; InterestRate = interestRate; StartDate = startDate; EndDate = endDate; Description = description;
        OutstandingBalance = amount; Status = LoanStatus.Active; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public decimal Amount { get; private set; } public decimal InterestRate { get; private set; } public DateOnly StartDate { get; private set; } public DateOnly EndDate { get; private set; }
    public string? Description { get; private set; } public decimal OutstandingBalance { get; private set; } public LoanStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? FullyPaidAt { get; private set; }
    
    public void Repay(decimal amount) 
    { 
        if (amount <= 0) throw new HrValidationException(nameof(amount), "Repayment amount must be positive.");
        if (amount > OutstandingBalance) throw new HrValidationException(nameof(amount), "Repayment amount cannot exceed outstanding balance.");
        OutstandingBalance -= amount;
        if (OutstandingBalance == 0) { Status = LoanStatus.Paid; FullyPaidAt = DateTimeOffset.UtcNow; }
    }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public enum LoanStatus { Active, Paid, Defaulted, Cancelled }

// Advanced time and attendance models
public sealed class TimeEntry
{
    private TimeEntry() { }
    public TimeEntry(string id, string tenantId, string employeeId, DateTimeOffset clockIn, DateTimeOffset? clockOut = null, string? location = null, string? notes = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        ClockIn = clockIn; ClockOut = clockOut; Location = location; Notes = notes;
        Status = clockOut.HasValue ? TimeEntryStatus.Completed : TimeEntryStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public DateTimeOffset ClockIn { get; private set; } public DateTimeOffset? ClockOut { get; private set; }
    public string? Location { get; private set; } public string? Notes { get; private set; }
    public TimeEntryStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    public decimal HoursWorked => ClockOut.HasValue ? (decimal)(ClockOut.Value - ClockIn).TotalHours : 0;
    
    public void RecordClockOut(DateTimeOffset clockOutTime, string? notes = null) 
    { 
        if (Status != TimeEntryStatus.Active) throw new HrValidationException(nameof(Status), "Only active time entries can be clocked out.");
        if (clockOutTime < ClockIn) throw new HrValidationException(nameof(clockOutTime), "Clock out time cannot precede clock in time.");
        ClockOut = clockOutTime; Status = TimeEntryStatus.Completed; Notes = notes ?? Notes;
    }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public enum TimeEntryStatus { Active, Completed, PendingApproval, Approved, Rejected }

public sealed class WorkSchedule
{
    private WorkSchedule() { }
    public WorkSchedule(string id, string tenantId, string employeeId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly effectiveFrom, DateOnly? effectiveTo = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        DayOfWeek = dayOfWeek; StartTime = startTime; EndTime = endTime; EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo;
        if (endTime < startTime) throw new HrValidationException(nameof(endTime), "End time cannot precede start time.");
        IsActive = true; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public DayOfWeek DayOfWeek { get; private set; } public TimeOnly StartTime { get; private set; } public TimeOnly EndTime { get; private set; }
    public DateOnly EffectiveFrom { get; private set; } public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } public DateTimeOffset CreatedAt { get; private set; }
    
    public void Deactivate() { IsActive = false; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public sealed class OvertimeRequest
{
    private OvertimeRequest() { }
    public OvertimeRequest(string id, string tenantId, string employeeId, DateTimeOffset startTime, DateTimeOffset endTime, decimal hours, string reason, string? description = null)
    {
        Id = Required(id, nameof(id)); TenantId = Required(tenantId, nameof(tenantId)); EmployeeId = Required(employeeId, nameof(employeeId));
        if (endTime < startTime) throw new HrValidationException(nameof(endTime), "End time cannot precede start time.");
        if (hours <= 0) throw new HrValidationException(nameof(hours), "Overtime hours must be positive.");
        StartTime = startTime; EndTime = endTime; Hours = hours; Reason = Required(reason, nameof(reason)); Description = description;
        Status = OvertimeStatus.Pending; CreatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!; public string TenantId { get; private set; } = null!; public string EmployeeId { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; } public DateTimeOffset EndTime { get; private set; }
    public decimal Hours { get; private set; } public string Reason { get; private set; } = null!; public string? Description { get; private set; }
    public OvertimeStatus Status { get; private set; } public DateTimeOffset CreatedAt { get; private set; } public DateTimeOffset? ApprovedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    
    public void Approve(string approvedBy) 
    { 
        if (Status != OvertimeStatus.Pending) throw new HrValidationException(nameof(Status), "Only pending overtime can be approved.");
        Status = OvertimeStatus.Approved; ApprovedBy = Required(approvedBy, nameof(approvedBy)); ApprovedAt = DateTimeOffset.UtcNow;
    }
    public void Reject() { if (Status != OvertimeStatus.Pending) throw new HrValidationException(nameof(Status), "Only pending overtime can be rejected."); Status = OvertimeStatus.Rejected; }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new HrValidationException(name, $"{name} is required.") : value.Trim();
}

public enum OvertimeStatus { Pending, Approved, Rejected, Cancelled }

public sealed class HrDbContext(DbContextOptions<HrDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>(); 
    public DbSet<Employment> Employments => Set<Employment>(); 
    public DbSet<HrChecklistItem> ChecklistItems => Set<HrChecklistItem>(); 
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>(); 
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>(); 
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>(); 
    public DbSet<LeaveDecision> LeaveDecisions => Set<LeaveDecision>(); 
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>(); 
    public DbSet<HrAccessReviewRequest> AccessReviewRequests => Set<HrAccessReviewRequest>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>(); 
    public DbSet<PerformanceGoal> PerformanceGoals => Set<PerformanceGoal>(); 
    public DbSet<PerformanceFeedback> PerformanceFeedbacks => Set<PerformanceFeedback>(); 
    public DbSet<CompetencyAssessment> CompetencyAssessments => Set<CompetencyAssessment>(); 
    public DbSet<ReviewCycle> ReviewCycles => Set<ReviewCycle>(); 
    public DbSet<AppraisalCommittee> AppraisalCommittees => Set<AppraisalCommittee>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<DisciplinaryAction> DisciplinaryActions => Set<DisciplinaryAction>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<HrPayrollRecord> HrPayrollRecords => Set<HrPayrollRecord>();
    public DbSet<PayrollAdvance> PayrollAdvances => Set<PayrollAdvance>();
    
    protected override void OnModelCreating(ModelBuilder b)
    { 
        // Table names use hr_ prefix instead of schema (SQLite compatibility)
        b.Entity<Employee>().HasIndex(x => new { x.TenantId, x.EmployeeNumber }).IsUnique(); 
        b.Entity<LeavePolicy>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique(); 
        b.Entity<LeaveBalance>().HasIndex(x => new { x.TenantId, x.EmployeeId, x.PolicyId, x.Year }).IsUnique(); 
        b.Entity<PerformanceReview>().HasIndex(x => new { x.TenantId, x.EmployeeId, x.ReviewCycleId }); 
        b.Entity<ReviewCycle>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique(); 
        b.Entity<JobPosting>().HasIndex(x => new { x.TenantId, x.Status });
        b.Entity<JobApplication>().HasIndex(x => new { x.TenantId, x.JobPostingId, x.Status });
        b.Entity<DisciplinaryAction>().HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
        
        foreach (var t in new[] { typeof(Employee), typeof(Employment), typeof(HrChecklistItem), typeof(LeavePolicy), typeof(LeaveBalance), typeof(LeaveRequest), typeof(LeaveDecision), typeof(EmployeeDocument), typeof(HrAccessReviewRequest), typeof(PerformanceReview), typeof(PerformanceGoal), typeof(PerformanceFeedback), typeof(CompetencyAssessment), typeof(ReviewCycle), typeof(AppraisalCommittee), typeof(JobPosting), typeof(JobApplication), typeof(Interview), typeof(JobOffer), typeof(DisciplinaryAction), typeof(PayrollRecord), typeof(HrPayrollRecord), typeof(EmployeeLoan), typeof(TimeEntry), typeof(WorkSchedule), typeof(OvertimeRequest), typeof(PayrollAdvance) }) 
        { 
            var tableName = t.Name switch
        {
            "HrChecklistItem" => "ChecklistItems",
            "HrAccessReviewRequest" => "AccessReviewRequests",
            "HrPayrollRecord" => "HrPayrollRecords",
            "TimeEntry" => "TimeEntries",
            "LeavePolicy" => "LeavePolicies",
            _ => t.Name + "s"
        };
            b.Entity(t).ToTable($"hr_{tableName}");
            b.Entity(t).Property<string>("TenantId").HasMaxLength(128).IsRequired(); 
            b.Entity(t).Property<string>("Id").HasMaxLength(128); 
        } 
        
        b.Entity<Employee>().Property(x => x.FirstName).IsRequired(); 
        b.Entity<Employee>().Property(x => x.LastName).IsRequired(); 
    }
}