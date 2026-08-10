using Microsoft.Extensions.Configuration;

namespace OnePage.Platform;

public sealed record TrustedApiCredential(string UserId, IReadOnlySet<string> AllowedTenantIds);

public interface ITrustedApiCredentialResolver
{
    TrustedApiCredential? Resolve(string? apiKey);
}

public sealed class ConfigurationApiCredentialResolver(IConfiguration configuration) : ITrustedApiCredentialResolver
{
    public TrustedApiCredential? Resolve(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

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
