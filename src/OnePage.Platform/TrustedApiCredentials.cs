using Microsoft.Extensions.Configuration;

namespace OnePage.Platform;

/// <summary>
/// Represents a trusted API credential with user and tenant access information
/// </summary>
public sealed record TrustedApiCredential(string UserId, IReadOnlySet<string> AllowedTenantIds)
{
    /// <summary>
    /// Flag indicating if this credential belongs to a super admin user
    /// </summary>
    public bool IsSuperAdmin { get; init; } = false;
}

public interface ITrustedApiCredentialResolver
{
    TrustedApiCredential? Resolve(string? apiKey);
}

/// <summary>
/// Configuration-based credential resolver
/// </summary>
public sealed class ConfigurationApiCredentialResolver(IConfiguration configuration) : ITrustedApiCredentialResolver
{
    public TrustedApiCredential? Resolve(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        // Check for super admin key first
        if (SuperAdmin.IsSuperAdminApiKey(apiKey))
        {
            // Super admin can access all tenants - represented by wildcard
            return new TrustedApiCredential(SuperAdmin.UserId, new HashSet<string> { "*" }) { IsSuperAdmin = true };
        }

        var credential = configuration.GetSection("OnePage:ApiCredentials").GetSection(apiKey.Trim());
        var userId = credential["UserId"];
        var tenantIds = credential.GetSection("TenantIds").GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToHashSet(StringComparer.Ordinal);

        return string.IsNullOrWhiteSpace(userId) || tenantIds.Count == 0
            ? null
            : new TrustedApiCredential(userId.Trim(), tenantIds);
    }
}
