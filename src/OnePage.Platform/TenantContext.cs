namespace OnePage.Platform;

public sealed record TenantContext
{
    private TenantContext(string userId, string tenantId, string? legalEntityId, string? scope, string correlationId)
    {
        UserId = userId; TenantId = tenantId; LegalEntityId = legalEntityId; Scope = scope; CorrelationId = correlationId;
    }

    public string UserId { get; }
    public string TenantId { get; }
    public string? LegalEntityId { get; }
    public string? Scope { get; }
    public string CorrelationId { get; }

    public static TenantContext Create(string? userId, string? tenantId, string? correlationId, string? legalEntityId = null, string? scope = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new TenantContextValidationException(nameof(userId), "User ID is required.");
        if (string.IsNullOrWhiteSpace(tenantId)) throw new TenantContextValidationException(nameof(tenantId), "Tenant ID is required.");
        if (string.IsNullOrWhiteSpace(correlationId)) throw new TenantContextValidationException(nameof(correlationId), "Correlation ID is required.");
        return new TenantContext(userId.Trim(), tenantId.Trim(), Optional(legalEntityId), Optional(scope), correlationId.Trim());
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class TenantContextValidationException(string parameterName, string message) : ArgumentException(message, parameterName);
