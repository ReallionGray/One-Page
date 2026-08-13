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
        definitions.TryGetValue((context.TenantId, key), out var d);
        if (d is null) return EntitlementDecision.Deny(key, null, EntitlementDenialReason.Missing, historicalRead);
        if (d.State is not (EntitlementState.Available or EntitlementState.Trial or EntitlementState.Active) && !historicalRead)
            return EntitlementDecision.Deny(key, d, EntitlementDenialReason.StateDisallowsWrite, false);
        if (d.Limit is not null && d.Usage + requestedUsage > d.Limit)
            return EntitlementDecision.Deny(key, d, EntitlementDenialReason.LimitExceeded, historicalRead);
        return EntitlementDecision.Allow(d, historicalRead);
    }
}
