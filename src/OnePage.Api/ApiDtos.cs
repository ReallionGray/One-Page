using OnePage.Hr;

namespace OnePage.Api;

// Top-level DTOs/commands used by endpoint handlers
public record CreateAssetCommand(string Id, string Tag, string Name, string? Description, string? LocationId, string? CustodianEmployeeId, string? LegalEntityId, string? BranchId, string? DepartmentId);
public record AssignAssetCommand(string EmployeeId);
public record DisposeAssetCommand(string Reason);
public record DecideApprovalCommand(bool Approve, string? Comment);

public record CreatePurchaseOrderCommand(string Id, string Supplier, decimal TotalAmount);
public record CreateInventoryItemCommand(string Id, string Sku, string Name, decimal Quantity);
public record AdjustInventoryCommand(decimal Delta);
public record SaleLineDto(string Sku, decimal Quantity);
public record CreatePosSaleCommand(string Id, string? RegisterId, decimal Total, IEnumerable<SaleLineDto>? Lines);
public record CreateJournalEntryCommand(string Id, string Reference);

// HR DTOs
public record CreateEmployeeCommand(string Id, string FirstName, string LastName, string Email, string? DepartmentId = null, string? Position = null, decimal Salary = 0, DateTimeOffset? HireDate = null);
public record UpdateEmployeeCommand(string? FirstName = null, string? LastName = null, string? Email = null, string? DepartmentId = null, string? Position = null, decimal? Salary = null);

// Advanced HR DTOs
public record CreateAdvancedEmployeeCommand(string Id, string EmployeeNumber, string FirstName, string LastName, string? Email = null, string? Phone = null, string? GovernmentId = null);
public record CreateEmploymentCommand(string Id, string? EmployeeId = null, DateOnly EffectiveFrom = default, string LegalEntityId = "", string DepartmentId = "", string Position = "", string? ManagerEmployeeId = null, string LocationId = "", EmploymentStatus Status = EmploymentStatus.Active, DateOnly? EffectiveTo = null);
public record CreateLeavePolicyCommand(string Id, string Code, string Name, decimal AnnualEntitlement, bool AllowCarryover = false);
public record CreateLeaveBalanceCommand(string Id, string EmployeeId, string PolicyId, int Year, decimal EntitledDays);
public record CreateLeaveRequestCommand(string Id, string EmployeeId, string PolicyId, DateOnly StartDate, DateOnly EndDate, decimal Days, string Reason);
public record DecideLeaveCommand(bool Approve, string? Comment = null);
public record CreateChecklistItemCommand(string Id, string EmployeeId, string Kind, string Title, string OwnerUserId, DateOnly? DueDate = null);
public record CompleteCheckItemCommand(string Evidence);
public record CreateEmployeeDocumentCommand(string Id, string EmployeeId, string DocumentType, string FileReference, DateOnly? ExpiresOn = null);
public record OffboardEmployeeCommand(DateOnly EffectiveDate);

// Performance management DTOs
public record CreatePerformanceReviewCommand(string Id, string EmployeeId, string ReviewCycleId, string Framework, DateOnly ReviewPeriodStart, DateOnly ReviewPeriodEnd);
public record CreatePerformanceGoalCommand(string Id, string EmployeeId, string PerformanceReviewId, string Title, string Description, decimal TargetValue, DateOnly DueDate);
public record CreatePerformanceFeedbackCommand(string Id, string PerformanceReviewId, string FromEmployeeId, string ToEmployeeId, string FeedbackType, string Content, bool IsAnonymous = false);
public record CreateCompetencyAssessmentCommand(string Id, string PerformanceReviewId, string CompetencyName, string CompetencyDescription, decimal Score, string? Comments = null);
public record CreateReviewCycleCommand(string Id, string Name, string Description, DateOnly StartDate, DateOnly EndDate, string Framework);
public record CreateAppraisalCommitteeCommand(string Id, string Name, string Description, IReadOnlyList<string> MemberEmployeeIds);

// Recruitment DTOs
public record CreateJobPostingCommand(string Id, string Title, string Description, string DepartmentId, string LocationId, string? Requirements = null, string? Responsibilities = null, decimal? MinSalary = null, decimal? MaxSalary = null);
public record CreateJobApplicationCommand(string Id, string JobPostingId, string CandidateName, string CandidateEmail, string? CandidatePhone = null, string? ResumeUrl = null, string? CoverLetter = null);
public record CreateInterviewCommand(string Id, string JobApplicationId, string InterviewerEmployeeId, DateTimeOffset ScheduledDateTime, string? Location = null, string? Notes = null);
public record CreateJobOfferCommand(string Id, string JobApplicationId, decimal Salary, string? Benefits = null, DateTimeOffset? StartDate = null);

// Recruitment lifecycle DTOs
public record UpdateJobApplicationStatusCommand(string Status);
public record CompleteInterviewCommand(string? Notes = null);

// Performance lifecycle DTOs
public record CompletePerformanceReviewCommand(decimal OverallScore, string? ManagerComments = null);
public record AddEmployeeCommentsCommand(string Comments);
public record UpdateGoalProgressCommand(decimal ActualValue, string Status);

// Payroll enhancement DTOs
public record CreateHrPayrollRecordCommand(string Id, string EmployeeId, DateOnly PayPeriodStart, DateOnly PayPeriodEnd, decimal GrossPay, decimal TaxDeduction, decimal PensionDeduction, decimal NetPay, string Currency = "USD");
public record CreateEmployeeLoanCommand(string Id, string EmployeeId, decimal Amount, decimal InterestRate, DateOnly StartDate, DateOnly EndDate, string? Description = null);
public record RepayEmployeeLoanCommand(decimal Amount);

// Time and attendance DTOs
public record CreateTimeEntryCommand(string Id, string EmployeeId, DateTimeOffset ClockIn, DateTimeOffset? ClockOut = null, string? Location = null, string? Notes = null);
public record ClockOutCommand(DateTimeOffset ClockOutTime, string? Notes = null);
public record CreateWorkScheduleCommand(string Id, string EmployeeId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
public record CreateOvertimeRequestCommand(string Id, string EmployeeId, DateTimeOffset StartTime, DateTimeOffset EndTime, decimal Hours, string Reason, string? Description = null);

// Disciplinary DTOs
public record CreateDisciplinaryActionCommand(string Id, string EmployeeId, string ActionType, string Severity, string Reason, string Description, DateOnly EffectiveDate, DateOnly? ExpiryDate = null);
public record ResolveDisciplinaryActionCommand(string? ResolutionNotes = null);

// Employee response DTOs (with sensitive field redaction)
public record EmployeeResponse(string Id, string TenantId, string EmployeeNumber, string FirstName, string LastName, string? Email, string? Phone, string? GovernmentId, bool IsActive, DateOnly? TerminationDate, DateTimeOffset CreatedAt);

// Payroll DTOs
public record CreatePayrollRecordCommand(string Id, string EmployeeId, decimal Amount, string Currency, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, string? Description = null);
public record RunPayrollCommand(string Currency, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd);
public record ProcessPayrollWithCalculationsCommand(decimal TaxRate = 0.2m, decimal PensionRate = 0.05m);
public record AddBonusCommand(decimal BonusAmount);
public record AddDeductionCommand(decimal DeductionAmount);
public record SetTaxDetailsCommand(string TaxCode, decimal TaxRate);
public record SetPensionDetailsCommand(string PensionScheme, decimal PensionRate);
public record GeneratePayslipCommand(string PayslipUrl);

// Dashboard analytics
public record ScheduleEvent(string Date, string Type, string Title, string? Description, string Status);
public record DashboardChartSegment(string Label, decimal Value, string? Color = null);

// Workflow management DTOs
public record CreateWorkflowStep(string ApproverType, string ApproverValue, bool CanSkip = false);
public record CreateWorkflowDefinitionCommand(string Name, string ResourceType, string TriggerType, decimal? TriggerAmount, bool IsActive, string? Description, decimal? TriggerQuantity, IList<CreateWorkflowStep>? Steps);
public record UpdateWorkflowDefinitionCommand(string Name, string? Description, string TriggerType, decimal? TriggerAmount, bool IsActive, decimal? TriggerQuantity, IList<CreateWorkflowStep>? Steps);
