using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IWorkflowRepository
{
    Task<WorkflowDefinition> CreateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowStep>> ListStepsAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task ReplaceStepsAsync(string workflowDefinitionId, IList<WorkflowStep> steps, CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds the active workflow definition for a tenant that matches the given resource type
    /// and trigger amount. A definition matches when it is active, its ResourceType matches,
    /// and either (a) its TriggerType is "always" or (b) its TriggerType is "amount" and
    /// the supplied amount is >= the definition's TriggerAmount, or (c) its TriggerType is
    /// "quantity" and the supplied quantity is >= the definition's TriggerQuantity.
    /// </summary>
    Task<WorkflowDefinition?> FindMatchingAsync(string tenantId, string resourceType, decimal? triggerAmount, decimal? triggerQuantity = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets an active workflow definition together with its ordered steps in a single round-trip.
    /// Returns null if the definition does not exist, is inactive, or has no steps.
    /// </summary>
    Task<(WorkflowDefinition definition, IReadOnlyList<WorkflowStep> steps)?> GetWithStepsAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class WorkflowRepository(OrganizationDbContext db) : IWorkflowRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task<WorkflowDefinition> CreateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _db.WorkflowDefinitions.Add(definition);
        await _db.SaveChangesAsync(cancellationToken);
        return definition;
    }

    public Task<WorkflowDefinition?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.WorkflowDefinitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _db.WorkflowDefinitions.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.ResourceType).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowStep>> ListStepsAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _db.WorkflowSteps.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == workflowDefinitionId)
            .OrderBy(x => x.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _db.WorkflowDefinitions.Update(definition);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var definition = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null) return;
        // Delete associated steps first
        var steps = await _db.WorkflowSteps.Where(x => x.WorkflowDefinitionId == id).ToListAsync(cancellationToken);
        if (steps.Count > 0) _db.WorkflowSteps.RemoveRange(steps);
        _db.WorkflowDefinitions.Remove(definition);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceStepsAsync(string workflowDefinitionId, IList<WorkflowStep> steps, CancellationToken cancellationToken = default)
    {
        var existing = await _db.WorkflowSteps.Where(x => x.WorkflowDefinitionId == workflowDefinitionId).ToListAsync(cancellationToken);
        if (existing.Count > 0) _db.WorkflowSteps.RemoveRange(existing);
        if (steps.Count > 0) _db.WorkflowSteps.AddRange(steps);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkflowDefinition?> FindMatchingAsync(string tenantId, string resourceType, decimal? triggerAmount, decimal? triggerQuantity = null, CancellationToken cancellationToken = default)
    {
        var query = _db.WorkflowDefinitions.AsNoTracking()
            .Where(w => w.TenantId == tenantId
                && w.ResourceType == resourceType
                && w.IsActive);

        // "always" triggers match any amount (including null).
        // "amount" triggers match when the supplied amount >= the threshold.
        // "quantity" triggers match when the supplied quantity >= the threshold.
        var alwaysMatch = query.Where(w => w.TriggerType == "always");
        var amountMatch = query.Where(w => w.TriggerType == "amount" && w.TriggerAmount.HasValue && triggerAmount >= w.TriggerAmount.Value);
        var quantityMatch = query.Where(w => w.TriggerType == "quantity" && w.TriggerQuantity.HasValue && triggerQuantity >= w.TriggerQuantity.Value);

        return await alwaysMatch.Concat(amountMatch).Concat(quantityMatch)
            .OrderByDescending(w => w.TriggerType == "amount" ? 1 : 0)
            .ThenByDescending(w => w.TriggerType == "quantity" ? 1 : 0)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(WorkflowDefinition definition, IReadOnlyList<WorkflowStep> steps)?> GetWithStepsAsync(string id, CancellationToken cancellationToken = default)
    {
        var definition = await _db.WorkflowDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (definition is null || !definition.IsActive) return null;

        var steps = await _db.WorkflowSteps.AsNoTracking()
            .Where(x => x.WorkflowDefinitionId == id)
            .OrderBy(x => x.StepNumber)
            .ToListAsync(cancellationToken);

        if (steps.Count == 0) return null;

        return (definition, steps);
    }
}
