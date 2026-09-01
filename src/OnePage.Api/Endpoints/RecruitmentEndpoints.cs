using OnePage.Platform;
using OnePage.Hr;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class RecruitmentEndpoints
{
    public static void MapRecruitmentEndpoints(this WebApplication app)
    {
        // Create job posting
        app.MapPost("/api/v1/hr/recruitment/jobs", async (CreateJobPostingCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var posting = await hr.CreateJobPostingAsync(new JobPosting(
                c.Id,
                current.TenantId,
                c.Title,
                c.Description,
                c.DepartmentId,
                c.LocationId,
                c.Requirements,
                c.Responsibilities,
                c.MinSalary,
                c.MaxSalary), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.recruitment.job.create:{posting.Id}", 
                "jobposting", 
                posting.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/recruitment/jobs/{posting.Id}", posting);
        });

        // Get job posting
        app.MapGet("/api/v1/hr/recruitment/jobs/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var posting = await hr.GetJobPostingAsync(id, ct);
            if (posting is null) return Results.NotFound();
            
            return Results.Ok(posting);
        });

        // Get job postings by status
        app.MapGet("/api/v1/hr/recruitment/jobs/status/{status}", async (string status, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var jobStatus = Enum.Parse<JobStatus>(status);
            var postings = await hr.GetJobPostingsByStatusAsync(current.TenantId, jobStatus, ct);
            return Results.Ok(postings);
        });

        // Create job application
        app.MapPost("/api/v1/hr/recruitment/applications", async (CreateJobApplicationCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var application = await hr.CreateJobApplicationAsync(new JobApplication(
                c.Id,
                current.TenantId,
                c.JobPostingId,
                c.CandidateName,
                c.CandidateEmail,
                c.CandidatePhone,
                c.ResumeUrl,
                c.CoverLetter), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.recruitment.application.create:{application.Id}", 
                "jobapplication", 
                application.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/recruitment/applications/{application.Id}", application);
        });

        // Get job application
        app.MapGet("/api/v1/hr/recruitment/applications/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var application = await hr.GetJobApplicationAsync(id, ct);
            if (application is null) return Results.NotFound();
            
            return Results.Ok(application);
        });

        // Get job applications by posting
        app.MapGet("/api/v1/hr/recruitment/jobs/{jobPostingId}/applications", async (string jobPostingId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var applications = await hr.GetJobApplicationsByPostingAsync(jobPostingId, ct);
            return Results.Ok(applications);
        });

        // Create interview
        app.MapPost("/api/v1/hr/recruitment/interviews", async (CreateInterviewCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var interview = await hr.CreateInterviewAsync(new Interview(
                c.Id,
                current.TenantId,
                c.JobApplicationId,
                c.InterviewerEmployeeId,
                c.ScheduledDateTime,
                c.Location,
                c.Notes), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.recruitment.interview.create:{interview.Id}", 
                "interview", 
                interview.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/recruitment/interviews/{interview.Id}", interview);
        });

        // Get interviews by application
        app.MapGet("/api/v1/hr/recruitment/applications/{jobApplicationId}/interviews", async (string jobApplicationId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var interviews = await hr.GetInterviewsByApplicationAsync(jobApplicationId, ct);
            return Results.Ok(interviews);
        });

        // Create job offer
        app.MapPost("/api/v1/hr/recruitment/offers", async (CreateJobOfferCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var offer = await hr.CreateJobOfferAsync(new JobOffer(
                c.Id,
                current.TenantId,
                c.JobApplicationId,
                c.Salary,
                c.Benefits,
                c.StartDate), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.recruitment.offer.create:{offer.Id}", 
                "joboffer", 
                offer.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/recruitment/offers/{offer.Id}", offer);
        });

        // Get job offer
        app.MapGet("/api/v1/hr/recruitment/offers/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var offer = await hr.GetJobOfferAsync(id, ct);
            if (offer is null) return Results.NotFound();
            
            return Results.Ok(offer);
        });

        // Publish job posting
        app.MapPost("/api/v1/hr/recruitment/jobs/{id}/publish", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var posting = await hr.PublishJobPostingAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.job.publish:{posting.Id}", "jobposting", posting.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(posting);
        });

        // Close job posting
        app.MapPost("/api/v1/hr/recruitment/jobs/{id}/close", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var posting = await hr.CloseJobPostingAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.job.close:{posting.Id}", "jobposting", posting.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(posting);
        });

        // Update application status
        app.MapPut("/api/v1/hr/recruitment/applications/{id}/status", async (string id, UpdateJobApplicationStatusCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var status = Enum.Parse<ApplicationStatus>(c.Status);
            var application = await hr.UpdateJobApplicationStatusAsync(id, status, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.application.status:{application.Id}:{status}", "jobapplication", application.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(application);
        });

        // Complete interview
        app.MapPost("/api/v1/hr/recruitment/interviews/{id}/complete", async (string id, CompleteInterviewCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var interview = await hr.CompleteInterviewAsync(id, c.Notes, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.interview.complete:{interview.Id}", "interview", interview.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(interview);
        });

        // Cancel interview
        app.MapPost("/api/v1/hr/recruitment/interviews/{id}/cancel", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var interview = await hr.CancelInterviewAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.interview.cancel:{interview.Id}", "interview", interview.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(interview);
        });

        // Send job offer
        app.MapPost("/api/v1/hr/recruitment/offers/{id}/send", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var offer = await hr.SendJobOfferAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.offer.send:{offer.Id}", "joboffer", offer.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(offer);
        });

        // Accept job offer
        app.MapPost("/api/v1/hr/recruitment/offers/{id}/accept", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var offer = await hr.AcceptJobOfferAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.offer.accept:{offer.Id}", "joboffer", offer.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(offer);
        });

        // Reject job offer
        app.MapPost("/api/v1/hr/recruitment/offers/{id}/reject", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var offer = await hr.RejectJobOfferAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.offer.reject:{offer.Id}", "joboffer", offer.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(offer);
        });

        // Withdraw job offer
        app.MapPost("/api/v1/hr/recruitment/offers/{id}/withdraw", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.RecruitmentManage,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var offer = await hr.WithdrawJobOfferAsync(id, ct);
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.recruitment.offer.withdraw:{offer.Id}", "joboffer", offer.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(offer);
        });
    }
}