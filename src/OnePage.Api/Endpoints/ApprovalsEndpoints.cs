using OnePage.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace OnePage.Api.Endpoints;

public static class ApprovalsEndpoints
{
    public static void MapApprovalEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/approvals/{id}", async (string id, ITenantContextAccessor ctx, IApprovalRepository approvals, IWorkflowRepository workflows, IAuthorizationRepository authRepo, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var req = await approvals.GetAsync(id, ct);
            if (req is null) return Results.NotFound();
            if (!string.Equals(req.TenantId, current.TenantId, StringComparison.Ordinal))
                return Results.Problem(statusCode: 403, title: "Cross-tenant", detail: "Request does not belong to current tenant.");

            // Build a rich response that includes workflow details and step decisions.
            var response = await BuildApprovalResponse(req, approvals, workflows, authRepo, ct);
            return Results.Ok(response);
        });

        app.MapPost("/api/v1/approvals/{id}/decide", async (string id, DecideApprovalCommand c, ITenantContextAccessor ctx, IApprovalRepository approvals, IWorkflowRepository workflows, IAuthorizationRepository authRepo, IModuleAccessEvaluator moduleAccess, IAuditRepository audit, IAssetsRepository assets, IProcurementRepository procurement, IInventoryRepository inventory, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var req = await approvals.GetAsync(id, ct);
            if (req is null) return Results.NotFound();
            if (req.TenantId != current.TenantId)
                return Results.Problem(statusCode: 403, title: "Cross-tenant", detail: "Request does not belong to current tenant.");

            // Check permission only (approvals don't require specific subscription module)
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess,
                null, // No specific module required for approvals
                PermissionCatalog.ApprovalReview,
                cancellationToken: ct);
            if (accessResult is not null) return accessResult;

            // Resolve the workflow definition and current step (if any).
            var userRoles = await authRepo.GetUserRolesAsync(current.TenantId, current.UserId, ct);
            var workflowResult = await workflows.GetWithStepsAsync(req.WorkflowDefinitionId ?? "", ct);
            var definition = workflowResult?.definition;
            var steps = workflowResult?.steps ?? Array.Empty<WorkflowStep>();

            // Determine the current step's approver.
            var currentStep = steps.FirstOrDefault(s => s.StepNumber == req.CurrentStep) ??
                              // Fallback: if no workflow or step lookup fails, treat as direct approval.
                              (req.CurrentStep == 0 ? new WorkflowStep("direct", req.TenantId, req.WorkflowDefinitionId ?? "", 0, "user", current.UserId) : null);

            if (currentStep is null)
                return Results.Problem(statusCode: 409, title: "Workflow step not found", detail: $"Could not determine the current approver step for request {req.Id}.");

            // Verify the current user is authorised to act at the current step.
            var isAuthorized = currentStep.ApproverType switch
            {
                "role" => userRoles.Contains(currentStep.ApproverValue) || SuperAdmin.IsSuperAdmin(current.UserId),
                "user" => string.Equals(currentStep.ApproverValue, current.UserId, StringComparison.Ordinal) || SuperAdmin.IsSuperAdmin(current.UserId),
                _ => false
            };

            // If not directly authorised, check skip authority: if the current step
            // has CanSkip=true, the next step's approver may also act on this step.
            var isSkipApprover = false;
            if (!isAuthorized && currentStep.CanSkip && req.CurrentStep > 0)
            {
                var nextStep = steps.FirstOrDefault(s => s.StepNumber == req.CurrentStep + 1);
                if (nextStep is not null)
                {
                    isSkipApprover = nextStep.ApproverType switch
                    {
                        "role" => userRoles.Contains(nextStep.ApproverValue) || SuperAdmin.IsSuperAdmin(current.UserId),
                        "user" => string.Equals(nextStep.ApproverValue, current.UserId, StringComparison.Ordinal) || SuperAdmin.IsSuperAdmin(current.UserId),
                        _ => false
                    };
                }
            }

            isAuthorized = isAuthorized || isSkipApprover;

            // For direct approvals (no workflow), allow anyone with the permission (checked above).
            if (req.CurrentStep > 0 && !isAuthorized)
                return Results.Problem(statusCode: 403, title: "Not the current approver", detail: $"User {current.UserId} is not authorised to act at step {req.CurrentStep} of this workflow.");

            try
            {
                if (c.Approve)
                {
                    // Record the per-step decision.
                    var decisionId = Guid.NewGuid().ToString("N");
                    var stepNumber = Math.Max(req.CurrentStep, 1);
                    await approvals.CreateDecisionAsync(new ApprovalDecision(decisionId, req.TenantId, req.Id, stepNumber, current.UserId, "approved", c.Comment), ct);

                    // Determine if this is the final step of the workflow.
                    var nextStep = steps.FirstOrDefault(s => s.StepNumber == stepNumber + 1);
                    if (nextStep is not null)
                    {
                        // Advance to the next step — request stays "pending".
                        req.AdvanceStep(nextStep.StepNumber);
                    }
                    else
                    {
                        // Final step — complete the approval.
                        req.Approve(current.UserId, c.Comment);
                    }
                    await approvals.UpdateAsync(req, ct);

                    await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId,
                        $"approval.step.approve:{req.Id}:{stepNumber}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);

                    // Execute the resource action if the request is now fully approved.
                    if (req.Status == "approved")
                        await ExecuteResourceAction(req, current, assets, procurement, inventory, audit, ct);
                }
                else
                {
                    // Rejection: record the decision and complete as rejected.
                    var stepNumber = Math.Max(req.CurrentStep, 1);
                    await approvals.CreateDecisionAsync(new ApprovalDecision(Guid.NewGuid().ToString("N"), req.TenantId, req.Id, stepNumber, current.UserId, "rejected", c.Comment), ct);
                    req.Reject(current.UserId, c.Comment);
                    await approvals.UpdateAsync(req, ct);
                    await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId,
                        $"approval.reject:{req.Id}", "approval", req.Id, null, null, current.CorrelationId, null, null), ct);
                }

                var response = await BuildApprovalResponse(req, approvals, workflows, authRepo, ct);
                return Results.Ok(response);
            }
            catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "Invalid approval decision", detail: ex.Message); }
        });

        app.MapGet("/api/v1/approvals/{id}/decisions", async (string id, ITenantContextAccessor ctx, IApprovalRepository approvals, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var req = await approvals.GetAsync(id, ct);
            if (req is null) return Results.NotFound();
            if (!string.Equals(req.TenantId, current.TenantId, StringComparison.Ordinal))
                return Results.Problem(statusCode: 403, title: "Cross-tenant", detail: "Request does not belong to current tenant.");

            var decisions = await approvals.ListDecisionsAsync(req.Id, ct);
            return Results.Ok(decisions.Select(d => new { d.Id, d.StepNumber, d.ApproverUserId, d.Decision, d.Comment, d.CreatedAt }));
        });

        app.MapGet("/api/v1/platform/audit/export", async (ITenantContextAccessor ctx, IAuditRepository audit, IModuleAccessEvaluator moduleAccess, CancellationToken ct) =>
        {
            var current = ctx.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var accessResult = await ctx.RequireModuleAccess(
                moduleAccess,
                null,
                PermissionCatalog.ReportExport,
                cancellationToken: ct);
            if (accessResult is not null) return accessResult;
            var events = await audit.ExportTenantEventsAsync(current.TenantId, ct);
            return Results.Ok(events);
        });
    }

    /// <summary>
    /// Builds a rich response object for an approval request, including workflow
    /// definition name, ordered steps, current step's approver info, and existing
    /// step decisions.
    /// </summary>
    private static async Task<object> BuildApprovalResponse(ApprovalRequest req, IApprovalRepository approvals, IWorkflowRepository workflows, IAuthorizationRepository authRepo, CancellationToken ct)
    {
        var definition = req.WorkflowDefinitionId is not null
            ? await workflows.GetAsync(req.WorkflowDefinitionId, ct)
            : null;

        var steps = (definition?.Id is not null)
            ? await workflows.ListStepsAsync(definition.Id, ct)
            : Array.Empty<WorkflowStep>();

        var decisions = await approvals.ListDecisionsAsync(req.Id, ct);

        var currentStepInfo = req.CurrentStep > 0
            ? steps.FirstOrDefault(s => s.StepNumber == req.CurrentStep)
            : null;

        return new
        {
            req.Id,
            req.TenantId,
            req.ResourceType,
            req.ResourceId,
            req.RequestedBy,
            req.Reason,
            req.Status,
            req.DecidedBy,
            req.DecisionComment,
            req.CreatedAt,
            req.DecidedAt,
            req.WorkflowDefinitionId,
            WorkflowName = definition?.Name,
            req.CurrentStep,
            StepApprovers = steps.Select(s => new { s.StepNumber, s.ApproverType, s.ApproverValue, s.CanSkip }).ToArray(),
            CurrentStepApproverType = currentStepInfo?.ApproverType,
            CurrentStepApproverValue = currentStepInfo?.ApproverValue,
            CurrentStepCanSkip = currentStepInfo?.CanSkip,
            Decisions = decisions.Select(d => new { d.Id, d.StepNumber, d.ApproverUserId, d.Decision, d.Comment, d.CreatedAt }).ToArray()
        };
    }

    /// <summary>
    /// Executes the resource-specific action after an approval is fully approved.
    /// </summary>
    private static async Task ExecuteResourceAction(ApprovalRequest req, TenantContext current,
        IAssetsRepository assets, IProcurementRepository procurement, IInventoryRepository inventory, IAuditRepository audit, CancellationToken ct)
    {
        if (req.ResourceType == "asset.dispose" && !string.IsNullOrWhiteSpace(req.ResourceId))
        {
            var asset = await assets.GetAsync(req.ResourceId, ct);
            if (asset is not null)
            {
                asset.Dispose(current.UserId);
                await assets.UpdateAsync(asset, ct);
                await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId,
                    $"asset.dispose:approved:{asset.Id}", "asset", asset.Id, null, null, current.CorrelationId, null, null), ct);
            }
        }
        if (req.ResourceType == "purchase_order.approve" && !string.IsNullOrWhiteSpace(req.ResourceId))
        {
            var po = await procurement.GetAsync(req.ResourceId, ct);
            if (po is not null)
            {
                po.Approve(current.UserId);
                await procurement.UpdateAsync(po, ct);
                await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId,
                    $"purchase_order.approve:approved:{po.Id}", "procurement", po.Id, null, null, current.CorrelationId, null, null), ct);
            }
        }
        if (req.ResourceType == "inventory.adjust" && !string.IsNullOrWhiteSpace(req.ResourceId))
        {
            var item = await inventory.GetAsync(req.ResourceId, ct);
            if (item is not null)
            {
                if (decimal.TryParse(req.Reason, out var delta))
                {
                    item.Adjust(delta);
                    await inventory.UpdateAsync(item, ct);
                    await audit.AddAsync(new AuditEvent(Guid.NewGuid().ToString("N"), current.TenantId, current.UserId,
                        $"inventory.adjust:approved:{item.Id}", "inventory", item.Id, null, null, current.CorrelationId, null, null), ct);
                }
            }
        }
    }
}
