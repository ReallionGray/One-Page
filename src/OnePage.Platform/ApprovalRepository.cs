using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IApprovalRepository
{
    Task<ApprovalRequest> CreateAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalRequest?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(string tenantId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns pending approvals where the user is the current step's approver
    /// (role-based or user-based) or where the user is the requester (their own pending requests).
    /// Super-admin users see all pending approvals in the tenant.
    /// </summary>
    Task<IReadOnlyList<ApprovalRequest>> ListForUserAsync(string tenantId, string userId, IReadOnlySet<string> userRoles, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns completed (approved or rejected) approvals where the user was the requester,
    /// the final decider, or an approver at any workflow step.
    /// Super-admin users see all completed approvals in the tenant.
    /// </summary>
    Task<IReadOnlyList<ApprovalRequest>> ListCompletedAsync(string tenantId, string userId, IReadOnlySet<string> userRoles, CancellationToken cancellationToken = default);
    Task<ApprovalDecision> CreateDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalDecision>> ListDecisionsAsync(string approvalRequestId, CancellationToken cancellationToken = default);
}

public sealed class ApprovalRepository(OrganizationDbContext db) : IApprovalRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task<ApprovalRequest> CreateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<ApprovalRequest?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.ApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _db.ApprovalRequests.Update(request);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ApprovalRequests.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "pending")
            .ToListAsync(cancellationToken);
        return list.OrderBy(x => x.CreatedAt).ToList();
    }

    /// <summary>
    /// Returns pending approvals where the user is the current step's approver
    /// (role-based: user has the role named in the current step, or user-based:
    /// the approver value equals the user ID) or where the user is the requester
    /// (their own pending requests — so they can see the status of what they asked for).
    /// If the current step has CanSkip=true, the next step's approver is also included.
    /// </summary>
    public async Task<IReadOnlyList<ApprovalRequest>> ListForUserAsync(string tenantId, string userId, IReadOnlySet<string> userRoles, CancellationToken cancellationToken = default)
    {
        // Load the workflow steps for all pending requests in this tenant in a single query.
        var pendingRequests = await _db.ApprovalRequests.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "pending")
            .ToListAsync(cancellationToken);

        if (pendingRequests.Count == 0) return Array.Empty<ApprovalRequest>();

        // Super-admin users see all pending approvals in the tenant.
        if (userRoles.Any(r => RoleNames.IsSuperAdminRole(r)))
            return pendingRequests.OrderBy(x => x.CreatedAt).ToList();

        // Collect the IDs of requests that have a workflow definition attached.
        var workflowedIds = pendingRequests
            .Where(r => r.WorkflowDefinitionId != null)
            .Select(r => r.WorkflowDefinitionId!)
            .Distinct()
            .ToList();

        // Pre-load all steps for the relevant workflow definitions in one query.
        var allSteps = await _db.WorkflowSteps.AsNoTracking()
            .Where(s => workflowedIds.Contains(s.WorkflowDefinitionId))
            .ToListAsync(cancellationToken);

        var result = new List<ApprovalRequest>();

        foreach (var request in pendingRequests)
        {
            if (string.IsNullOrEmpty(request.WorkflowDefinitionId) || request.CurrentStep == 0)
            {
                // No workflow — direct approval. Show it to the requester so they can track it.
                result.Add(request);
            }
            else
            {
                // Workflow-managed. The user must be the current step's approver.
                var step = allSteps.FirstOrDefault(s => s.WorkflowDefinitionId == request.WorkflowDefinitionId && s.StepNumber == request.CurrentStep);
                if (step == null)
                {
                    // No matching step found — fall back to showing the requester their own request.
                    if (request.RequestedBy == userId)
                        result.Add(request);
                }
                else
                {
                    var isApprover = IsStepApprover(step, userId, userRoles);
                    // Check if the user is the current step's approver.
                    if (isApprover || request.RequestedBy == userId)
                        result.Add(request);
                    else
                    {
                        // Check skip: if current step has CanSkip, the next step's approver can also act.
                        if (step.CanSkip)
                        {
                            var nextStep = allSteps.FirstOrDefault(s => s.WorkflowDefinitionId == request.WorkflowDefinitionId && s.StepNumber == request.CurrentStep + 1);
                            if (nextStep != null && IsStepApprover(nextStep, userId, userRoles))
                                result.Add(request);
                        }
                    }
                }
            }
        }

        return result.OrderBy(x => x.CreatedAt).ToList();
    }

    private static bool IsStepApprover(WorkflowStep step, string userId, IReadOnlySet<string> userRoles)
    {
        return step.ApproverType switch
        {
            "role" => userRoles.Contains(step.ApproverValue),
            "user" => string.Equals(step.ApproverValue, userId, StringComparison.Ordinal),
            _ => false
        };
    }

    /// <summary>
    /// Returns completed (approved or rejected) approvals where the user was the
    /// requester, the final decider, or an approver at any workflow step (checked
    /// via the ApprovalDecision table). Super-admin users see all completed
    /// approvals in the tenant.
    /// </summary>
    public async Task<IReadOnlyList<ApprovalRequest>> ListCompletedAsync(string tenantId, string userId, IReadOnlySet<string> userRoles, CancellationToken cancellationToken = default)
    {
        // Super-admin users see all completed approvals in the tenant.
        if (userRoles.Any(r => RoleNames.IsSuperAdminRole(r)))
        {
            return await _db.ApprovalRequests.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status != "pending")
                .OrderByDescending(x => x.DecidedAt)
                .ToListAsync(cancellationToken);
        }
        var completedFromDecision = await _db.ApprovalDecisions
            .Where(d => d.ApproverUserId == userId)
            .Join(
                _db.ApprovalRequests.Where(r => r.TenantId == tenantId && r.Status != "pending"),
                d => d.ApprovalRequestId,
                r => r.Id,
                (d, r) => r
            )
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var completedFromRequest = await _db.ApprovalRequests.AsNoTracking()
            .Where(x => x.TenantId == tenantId
                && x.Status != "pending"
                && (x.RequestedBy == userId || x.DecidedBy == userId))
            .ToListAsync(cancellationToken);

        // Union by Id to avoid duplicates.
        var seen = new HashSet<string>();
        var result = new List<ApprovalRequest>();
        foreach (var r in completedFromRequest.Concat(completedFromDecision))
        {
            if (seen.Add(r.Id))
                result.Add(r);
        }

        return result.OrderByDescending(x => x.DecidedAt).ToList();
    }

    public async Task<ApprovalDecision> CreateDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        _db.ApprovalDecisions.Add(decision);
        await _db.SaveChangesAsync(cancellationToken);
        return decision;
    }

    public async Task<IReadOnlyList<ApprovalDecision>> ListDecisionsAsync(string approvalRequestId, CancellationToken cancellationToken = default)
    {
        var decisions = await _db.ApprovalDecisions.AsNoTracking()
            .Where(d => d.ApprovalRequestId == approvalRequestId)
            .ToListAsync(cancellationToken);
        return decisions.OrderBy(d => d.StepNumber).ThenBy(d => d.CreatedAt).ToList();
    }
}
