using OnePage.Platform;
using OnePage.Hr;
using Microsoft.AspNetCore.Builder;

namespace OnePage.Api.Endpoints;

public static class PerformanceEndpoints
{
    public static void MapPerformanceEndpoints(this WebApplication app)
    {
        // Create performance review
        app.MapPost("/api/v1/hr/performance/reviews", async (CreatePerformanceReviewCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var review = await hr.CreatePerformanceReviewAsync(new PerformanceReview(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.ReviewCycleId,
                Enum.Parse<ReviewFramework>(c.Framework),
                c.ReviewPeriodStart,
                c.ReviewPeriodEnd), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.performance.review.create:{review.Id}", 
                "performancereview", 
                review.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/performance/reviews/{review.Id}", review);
        });

        // Get performance review
        app.MapGet("/api/v1/hr/performance/reviews/{id}", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var review = await hr.GetPerformanceReviewAsync(id, ct);
            if (review is null) return Results.NotFound();
            
            return Results.Ok(review);
        });

        // Get performance reviews by employee
        app.MapGet("/api/v1/hr/performance/reviews/employee/{employeeId}", async (string employeeId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var reviews = await hr.GetPerformanceReviewsByEmployeeAsync(employeeId, ct);
            return Results.Ok(reviews);
        });

        // Create performance goal
        app.MapPost("/api/v1/hr/performance/goals", async (CreatePerformanceGoalCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var goal = await hr.CreatePerformanceGoalAsync(new PerformanceGoal(
                c.Id,
                current.TenantId,
                c.EmployeeId,
                c.PerformanceReviewId,
                c.Title,
                c.Description,
                c.TargetValue,
                c.DueDate), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.performance.goal.create:{goal.Id}", 
                "performancegoal", 
                goal.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/performance/goals/{goal.Id}", goal);
        });

        // Get performance goals by review
        app.MapGet("/api/v1/hr/performance/reviews/{reviewId}/goals", async (string reviewId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var goals = await hr.GetPerformanceGoalsByReviewAsync(reviewId, ct);
            return Results.Ok(goals);
        });

        // Create performance feedback
        app.MapPost("/api/v1/hr/performance/feedback", async (CreatePerformanceFeedbackCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var feedback = await hr.CreatePerformanceFeedbackAsync(new PerformanceFeedback(
                c.Id,
                current.TenantId,
                c.PerformanceReviewId,
                c.FromEmployeeId,
                c.ToEmployeeId,
                Enum.Parse<FeedbackType>(c.FeedbackType),
                c.Content,
                c.IsAnonymous), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.performance.feedback.create:{feedback.Id}", 
                "performancefeedback", 
                feedback.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/performance/feedback/{feedback.Id}", feedback);
        });

        // Get performance feedback by review
        app.MapGet("/api/v1/hr/performance/reviews/{reviewId}/feedback", async (string reviewId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var feedbacks = await hr.GetPerformanceFeedbacksByReviewAsync(reviewId, ct);
            return Results.Ok(feedbacks);
        });

        // Create competency assessment
        app.MapPost("/api/v1/hr/performance/competencies", async (CreateCompetencyAssessmentCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var assessment = await hr.CreateCompetencyAssessmentAsync(new CompetencyAssessment(
                c.Id,
                current.TenantId,
                c.PerformanceReviewId,
                c.CompetencyName,
                c.CompetencyDescription,
                c.Score,
                c.Comments), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.performance.competency.create:{assessment.Id}", 
                "competencyassessment", 
                assessment.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/performance/competencies/{assessment.Id}", assessment);
        });

        // Get competency assessments by review
        app.MapGet("/api/v1/hr/performance/reviews/{reviewId}/competencies", async (string reviewId, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var assessments = await hr.GetCompetencyAssessmentsByReviewAsync(reviewId, ct);
            return Results.Ok(assessments);
        });

        // Create review cycle
        app.MapPost("/api/v1/hr/performance/review-cycles", async (CreateReviewCycleCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var cycle = await hr.CreateReviewCycleAsync(new ReviewCycle(
                c.Id,
                current.TenantId,
                c.Name,
                c.Description,
                c.StartDate,
                c.EndDate,
                Enum.Parse<ReviewFramework>(c.Framework)), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.performance.reviewcycle.create:{cycle.Id}", 
                "reviewcycle", 
                cycle.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/performance/review-cycles/{cycle.Id}", cycle);
        });

        // Get active review cycles
        app.MapGet("/api/v1/hr/performance/review-cycles/active", async (ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeView,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var cycles = await hr.GetActiveReviewCyclesAsync(current.TenantId, ct);
            return Results.Ok(cycles);
        });

        // Create appraisal committee
        app.MapPost("/api/v1/hr/performance/appraisal-committees", async (CreateAppraisalCommitteeCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess, 
                EntitlementKeys.Modules.Hr, 
                PermissionCatalog.EmployeeUpdate,
                cancellationToken: ct);
            
            if (accessResult is not null) return accessResult;
            
            var committee = await hr.CreateAppraisalCommitteeAsync(new AppraisalCommittee(
                c.Id,
                current.TenantId,
                c.Name,
                c.Description,
                c.MemberEmployeeIds), ct);
            
            await audit.AddAsync(new AuditEvent(
                Guid.NewGuid().ToString("N"), 
                current.TenantId, 
                current.UserId, 
                $"hr.performance.appraisalcommittee.create:{committee.Id}", 
                "appraisalcommittee", 
                committee.Id, 
                null, 
                null, 
                current.CorrelationId, 
                null, 
                null), ct);
            
            return Results.Created($"/api/v1/hr/performance/appraisal-committees/{committee.Id}", committee);
        });

        // Submit performance review
        app.MapPost("/api/v1/hr/performance/reviews/{id}/submit", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Hr, PermissionCatalog.PerformanceManage, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            var review = await hr.SubmitPerformanceReviewAsync(id, ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.performance.review.submit:{review.Id}", "performancereview", review.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(review);
        });

        // Start performance review
        app.MapPost("/api/v1/hr/performance/reviews/{id}/start", async (string id, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Hr, PermissionCatalog.PerformanceManage, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            var review = await hr.StartPerformanceReviewAsync(id, ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.performance.review.start:{review.Id}", "performancereview", review.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(review);
        });

        // Complete performance review
        app.MapPost("/api/v1/hr/performance/reviews/{id}/complete", async (string id, CompletePerformanceReviewCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Hr, PermissionCatalog.PerformanceManage, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            var review = await hr.CompletePerformanceReviewAsync(id, c.OverallScore, c.ManagerComments, ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.performance.review.complete:{review.Id}", "performancereview", review.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(review);
        });

        // Add employee comments to review
        app.MapPost("/api/v1/hr/performance/reviews/{id}/employee-comments", async (string id, AddEmployeeCommentsCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Hr, PermissionCatalog.PerformanceManage, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            var review = await hr.AddEmployeeCommentsToReviewAsync(id, c.Comments, ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.performance.review.comments:{review.Id}", "performancereview", review.Id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(review);
        });

        // Update goal progress
        app.MapPost("/api/v1/hr/performance/goals/{id}/progress", async (string id, UpdateGoalProgressCommand c, ITenantContextAccessor ctx, IModuleAccessEvaluator moduleAccess, IHrRepository hr, IAuditRepository audit, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var accessResult = await ctx.RequireModuleAccess(moduleAccess, EntitlementKeys.Modules.Hr, PermissionCatalog.PerformanceManage, cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            var goal = await hr.UpdateGoalProgressAsync(id, c.ActualValue, Enum.Parse<GoalStatus>(c.Status), ct);
            await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId, $"hr.performance.goal.progress:{id}", "performancegoal", id, null, null, current.CorrelationId, null, null), ct);
            return Results.Ok(goal);
        });
    }
}