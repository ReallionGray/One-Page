using OnePage.Platform;

namespace OnePage.Platform.Tests;

public class PlatformContractsTests
{
    private static TenantContext Context(string tenant = "tenant-1") => TenantContext.Create("user-1", tenant, "corr-1");

    [Fact] public void TenantContext_requires_required_values()
    {
        Assert.Throws<TenantContextValidationException>(() => TenantContext.Create(null, "tenant", "correlation"));
        Assert.Throws<TenantContextValidationException>(() => TenantContext.Create("user", "", "correlation"));
        Assert.Throws<TenantContextValidationException>(() => TenantContext.Create("user", "tenant", " "));
    }

    [Fact] public void Entitlement_keys_have_typed_namespaces()
    {
        Assert.Equal("module.hr", EntitlementKeys.Modules.Hr.ToString());
        Assert.Equal("feature.reporting.advanced", EntitlementKeys.Features.AdvancedReporting.ToString());
        Assert.Equal("limit.users", EntitlementKeys.Limits.Users.ToString());
    }

    [Theory]
    [InlineData(EntitlementState.Available, true)] [InlineData(EntitlementState.Trial, true)] [InlineData(EntitlementState.Active, true)]
    [InlineData(EntitlementState.Suspended, false)] [InlineData(EntitlementState.GracePeriod, false)] [InlineData(EntitlementState.Expired, false)] [InlineData(EntitlementState.ReadOnly, false)]
    public void Evaluator_applies_state_rules(EntitlementState state, bool allowed)
    {
        var evaluator = new InMemoryEntitlementEvaluator(); evaluator.Set("tenant-1", new(EntitlementKeys.Modules.Hr, state));
        var decision = evaluator.Evaluate(Context(), EntitlementKeys.Modules.Hr);
        Assert.Equal(allowed, decision.Allowed);
        if (!allowed) Assert.Equal(EntitlementDenialReason.StateDisallowsWrite, decision.DenialReason);
    }

    [Fact] public void Historical_read_is_allowed_for_disabled_entitlement()
    {
        var evaluator = new InMemoryEntitlementEvaluator(); evaluator.Set("tenant-1", new(EntitlementKeys.Modules.Hr, EntitlementState.Expired));
        var decision = evaluator.Evaluate(Context(), EntitlementKeys.Modules.Hr, historicalRead: true);
        Assert.True(decision.Allowed); Assert.True(decision.HistoricalRead);
    }

    [Fact] public void Missing_entitlement_returns_typed_denial()
    {
        var decision = new InMemoryEntitlementEvaluator().Evaluate(Context(), EntitlementKeys.Modules.Payroll);
        Assert.False(decision.Allowed); Assert.Equal(EntitlementDenialReason.Missing, decision.DenialReason); Assert.Null(decision.State);
    }

    [Theory] [InlineData(5, 5, 0, true)] [InlineData(5, 5, 1, false)] [InlineData(5, 4, 1, true)]
    public void Numeric_limits_expose_usage_and_enforce_boundary(long limit, long usage, long requested, bool allowed)
    {
        var evaluator = new InMemoryEntitlementEvaluator(); evaluator.Set("tenant-1", new(EntitlementKeys.Limits.Users, EntitlementState.Active, Limit: limit, Usage: usage));
        var decision = evaluator.Evaluate(Context(), EntitlementKeys.Limits.Users, requested);
        Assert.Equal(allowed, decision.Allowed); Assert.Equal(limit, decision.Limit); Assert.Equal(usage, decision.Usage);
        if (!allowed) Assert.Equal(EntitlementDenialReason.LimitExceeded, decision.DenialReason);
    }
}
