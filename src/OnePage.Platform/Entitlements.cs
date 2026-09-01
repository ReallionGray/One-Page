using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public readonly record struct EntitlementKey(string Namespace, string Name)
{
    public override string ToString() => $"{Namespace}.{Name}";
    public static EntitlementKey Module(string name) => Create("module", name);
    public static EntitlementKey Feature(string name) => Create("feature", name);
    public static EntitlementKey Limit(string name) => Create("limit", name);
    private static EntitlementKey Create(string @namespace, string name) => string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Entitlement name is required.", nameof(name))
        : new(@namespace, name.Trim());
}

public static class EntitlementKeys
{
    public static class Modules
    {
        public static readonly EntitlementKey Hr = EntitlementKey.Module("hr");
        public static readonly EntitlementKey Payroll = EntitlementKey.Module("payroll");
        public static readonly EntitlementKey Procurement = EntitlementKey.Module("procurement");
        public static readonly EntitlementKey Assets = EntitlementKey.Module("assets");
        public static readonly EntitlementKey Pos = EntitlementKey.Module("pos");
        public static readonly EntitlementKey Inventory = EntitlementKey.Module("inventory");
        public static readonly EntitlementKey Finance = EntitlementKey.Module("finance");
        public static readonly EntitlementKey Reporting = EntitlementKey.Module("reporting");
    }
    public static class Features
    {
        public static readonly EntitlementKey AdvancedReporting = EntitlementKey.Feature("reporting.advanced");
        public static readonly EntitlementKey ThreeWayMatching = EntitlementKey.Feature("procurement.three_way_matching");
    }
    public static class Limits
    {
        public static readonly EntitlementKey Users = EntitlementKey.Limit("users");
        public static readonly EntitlementKey ActiveEmployees = EntitlementKey.Limit("active_employees");
        public static readonly EntitlementKey Branches = EntitlementKey.Limit("branches");
    }
}

public enum EntitlementState { Available, Trial, Active, Suspended, GracePeriod, Expired, ReadOnly }
public enum EntitlementDenialReason { Missing, StateDisallowsWrite, LimitExceeded }

public sealed record EntitlementDefinition(EntitlementKey Key, EntitlementState State, string? Source = null,
    DateTimeOffset? EffectiveAt = null, long? Limit = null, long Usage = 0);

public sealed record EntitlementDecision
{
    private EntitlementDecision(bool allowed, EntitlementKey key, EntitlementState? state, string? source,
        DateTimeOffset? effectiveAt, long? limit, long usage, EntitlementDenialReason? denialReason, bool historicalRead)
    { Allowed = allowed; Key = key; State = state; Source = source; EffectiveAt = effectiveAt; Limit = limit; Usage = usage; DenialReason = denialReason; HistoricalRead = historicalRead; }
    public bool Allowed { get; }
    public EntitlementKey Key { get; }
    public EntitlementState? State { get; }
    public string? Source { get; }
    public DateTimeOffset? EffectiveAt { get; }
    public long? Limit { get; }
    public long Usage { get; }
    public EntitlementDenialReason? DenialReason { get; }
    public bool HistoricalRead { get; }
    public static EntitlementDecision Allow(EntitlementDefinition d, bool historicalRead) => new(true, d.Key, d.State, d.Source, d.EffectiveAt, d.Limit, d.Usage, null, historicalRead);
    public static EntitlementDecision Deny(EntitlementKey key, EntitlementDefinition? d, EntitlementDenialReason reason, bool historicalRead) => new(false, key, d?.State, d?.Source, d?.EffectiveAt, d?.Limit, d?.Usage ?? 0, reason, historicalRead);
}

public interface IEntitlementEvaluator
{
    EntitlementDecision Evaluate(TenantContext context, EntitlementKey key, long requestedUsage = 0, bool historicalRead = false);
}

public sealed class InMemoryEntitlementEvaluator : IEntitlementEvaluator
{
    private readonly Dictionary<(string TenantId, EntitlementKey Key), EntitlementDefinition> definitions = new();
    public void Set(string tenantId, EntitlementDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        definitions[(tenantId.Trim(), definition.Key)] = definition;
    }
    public EntitlementDecision Evaluate(TenantContext context, EntitlementKey key, long requestedUsage = 0, bool historicalRead = false)
    {
        if (requestedUsage < 0) throw new ArgumentOutOfRangeException(nameof(requestedUsage), "Requested usage cannot be negative.");
        
        // Super admin bypasses all entitlement checks
        if (SuperAdmin.IsSuperAdmin(context.UserId))
        {
            return EntitlementDecision.Allow(
                new EntitlementDefinition(key, EntitlementState.Active, "super-admin"), 
                historicalRead);
        }
        
        definitions.TryGetValue((context.TenantId, key), out var d);
        if (d is null) return EntitlementDecision.Deny(key, null, EntitlementDenialReason.Missing, historicalRead);
        if (d.State is not (EntitlementState.Available or EntitlementState.Trial or EntitlementState.Active) && !historicalRead)
            return EntitlementDecision.Deny(key, d, EntitlementDenialReason.StateDisallowsWrite, false);
        if (d.Limit is not null && d.Usage + requestedUsage > d.Limit)
            return EntitlementDecision.Deny(key, d, EntitlementDenialReason.LimitExceeded, historicalRead);
        return EntitlementDecision.Allow(d, historicalRead);
    }
}

public interface IEntitlementRepository
{
    Task<EntitlementAssignment?> GetAssignmentAsync(string tenantId, EntitlementKey key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntitlementAssignment>> GetAssignmentsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task AddAssignmentAsync(EntitlementAssignment assignment, CancellationToken cancellationToken = default);
    Task UpdateAssignmentAsync(EntitlementAssignment assignment, CancellationToken cancellationToken = default);
}

public sealed class EntitlementRepository(OrganizationDbContext db) : IEntitlementRepository
{
    public Task<EntitlementAssignment?> GetAssignmentAsync(string tenantId, EntitlementKey key, CancellationToken cancellationToken = default) =>
        db.EntitlementAssignments.AsNoTracking()
            .SingleOrDefaultAsync(e => e.TenantId == tenantId && e.EntitlementNamespace == key.Namespace && e.EntitlementName == key.Name, cancellationToken);

    public async Task<IReadOnlyList<EntitlementAssignment>> GetAssignmentsForTenantAsync(string tenantId, CancellationToken cancellationToken = default) =>
        await db.EntitlementAssignments.AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task AddAssignmentAsync(EntitlementAssignment assignment, CancellationToken cancellationToken = default)
    {
        await db.EntitlementAssignments.AddAsync(assignment, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAssignmentAsync(EntitlementAssignment assignment, CancellationToken cancellationToken = default)
    {
        db.EntitlementAssignments.Update(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DatabaseEntitlementEvaluator : IEntitlementEvaluator
{
    private readonly IEntitlementRepository _repository;
    private readonly IEntitlementEvaluator? _fallbackEvaluator;

    public DatabaseEntitlementEvaluator(IEntitlementRepository repository, IEntitlementEvaluator? fallbackEvaluator = null)
    {
        _repository = repository;
        _fallbackEvaluator = fallbackEvaluator;
    }

    public async Task<EntitlementDecision> EvaluateAsync(TenantContext context, EntitlementKey key, long requestedUsage = 0, bool historicalRead = false, CancellationToken cancellationToken = default)
    {
        if (requestedUsage < 0) throw new ArgumentOutOfRangeException(nameof(requestedUsage), "Requested usage cannot be negative.");
        
        var assignment = await _repository.GetAssignmentAsync(context.TenantId, key, cancellationToken);
        if (assignment is null)
        {
            // Fall back to in-memory evaluator if available (for demo/backward compatibility)
            if (_fallbackEvaluator is not null)
            {
                return _fallbackEvaluator.Evaluate(context, key, requestedUsage, historicalRead);
            }
            return EntitlementDecision.Deny(key, null, EntitlementDenialReason.Missing, historicalRead);
        }

        var definition = assignment.ToDefinition();
        
        if (definition.State is not (EntitlementState.Available or EntitlementState.Trial or EntitlementState.Active) && !historicalRead)
            return EntitlementDecision.Deny(key, definition, EntitlementDenialReason.StateDisallowsWrite, false);
        
        if (definition.Limit is not null && definition.Usage + requestedUsage > definition.Limit)
            return EntitlementDecision.Deny(key, definition, EntitlementDenialReason.LimitExceeded, historicalRead);
        
        return EntitlementDecision.Allow(definition, historicalRead);
    }

    public EntitlementDecision Evaluate(TenantContext context, EntitlementKey key, long requestedUsage = 0, bool historicalRead = false)
    {
        // Synchronous version that falls back to in-memory evaluator
        if (_fallbackEvaluator is not null)
        {
            return _fallbackEvaluator.Evaluate(context, key, requestedUsage, historicalRead);
        }
        
        // For synchronous calls, we need to run the async version synchronously
        try
        {
            return EvaluateAsync(context, key, requestedUsage, historicalRead).GetAwaiter().GetResult();
        }
        catch
        {
            return EntitlementDecision.Deny(key, null, EntitlementDenialReason.Missing, historicalRead);
        }
    }
}

public enum ModuleAccessDenialReason
{
    SubscriptionRequired,
    SubscriptionInactive,
    PermissionRequired,
    SubscriptionAndPermissionRequired
}

public sealed record ModuleAccessDecision(
    bool Allowed, 
    ModuleAccessDenialReason? DenialReason = null, 
    string? SubscriptionPlan = null,
    string? RequiredPermission = null)
{
    public static ModuleAccessDecision Allow() => new(true);
    public static ModuleAccessDecision Deny(ModuleAccessDenialReason reason, string? subscriptionPlan = null, string? requiredPermission = null) 
        => new(false, reason, subscriptionPlan, requiredPermission);
}

public interface IModuleAccessEvaluator
{
    Task<ModuleAccessDecision> EvaluateModuleAccessAsync(
        TenantContext context, 
        EntitlementKey moduleKey, 
        PermissionKey? requiredPermission = null,
        AuthorizationScope? scope = null,
        decimal? amount = null,
        CancellationToken cancellationToken = default);
}

public sealed class ModuleAccessEvaluator : IModuleAccessEvaluator
{
    private readonly IEntitlementEvaluator _entitlementEvaluator;
    private readonly IAuthorizationEvaluator _authorizationEvaluator;
    private readonly ITenantRepository _tenantRepository;

    public ModuleAccessEvaluator(
        IEntitlementEvaluator entitlementEvaluator,
        IAuthorizationEvaluator authorizationEvaluator,
        ITenantRepository tenantRepository)
    {
        _entitlementEvaluator = entitlementEvaluator;
        _authorizationEvaluator = authorizationEvaluator;
        _tenantRepository = tenantRepository;
    }

    public async Task<ModuleAccessDecision> EvaluateModuleAccessAsync(
        TenantContext context, 
        EntitlementKey moduleKey, 
        PermissionKey? requiredPermission = null,
        AuthorizationScope? scope = null,
        decimal? amount = null,
        CancellationToken cancellationToken = default)
    {
        // Skip subscription check for system module (permission-only checks).
        // EntitlementKey.Module("system") produces ("module", "system"); the
        // system module is used for permission-only checks that have no subscription.
        bool isSystemModule = moduleKey == EntitlementKey.Module("system");
        
        if (!isSystemModule)
        {
            // First check subscription (entitlement)
            var entitlementDecision = _entitlementEvaluator.Evaluate(context, moduleKey, 0, false);
            
            if (!entitlementDecision.Allowed)
            {
                // Get tenant subscription plan for better error message
                var tenant = await _tenantRepository.GetAsync(context.TenantId, cancellationToken);
                var planName = tenant?.SubscriptionPlan.ToString() ?? "Unknown";
                
                return ModuleAccessDecision.Deny(
                    ModuleAccessDenialReason.SubscriptionRequired, 
                    planName, 
                    requiredPermission?.ToString());
            }
        }

        // If no specific permission is required, subscription is sufficient
        if (requiredPermission is null)
        {
            return ModuleAccessDecision.Allow();
        }

        // Check user permission for the specific action
        var authRequest = new AuthorizationRequest(context, requiredPermission.Value, scope, amount);
        var authDecision = await _authorizationEvaluator.AuthorizeAsync(authRequest, cancellationToken);

        if (!authDecision.Allowed)
        {
            var tenant = await _tenantRepository.GetAsync(context.TenantId, cancellationToken);
            var planName = tenant?.SubscriptionPlan.ToString() ?? "Unknown";
            
            return ModuleAccessDecision.Deny(
                ModuleAccessDenialReason.PermissionRequired, 
                planName, 
                requiredPermission.ToString());

        }

        return ModuleAccessDecision.Allow();
    }
}
