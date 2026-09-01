namespace OnePage.Platform;

/// <summary>
/// Super admin user has unrestricted access across all tenants and modules.
/// Used for initial system setup and administrative tasks.
/// </summary>
public static class SuperAdmin
{
    /// <summary>
    /// Special user ID for super admin
    /// </summary>
    public const string UserId = "super-admin";
    
    /// <summary>
    /// Special API key for super admin (can be configured via appsettings)
    /// </summary>
    public const string ApiKey = "super-admin-key";
    
    /// <summary>
    /// Special tenant ID for super admin operations
    /// </summary>
    public const string TenantId = "system";
    
    /// <summary>
    /// Check if a user is the super admin
    /// </summary>
    public static bool IsSuperAdmin(string? userId) => string.Equals(userId, UserId, StringComparison.Ordinal);
    
    /// <summary>
    /// Check if an API key is the super admin key
    /// </summary>
    public static bool IsSuperAdminApiKey(string? apiKey) => string.Equals(apiKey, ApiKey, StringComparison.Ordinal);
    
    /// <summary>
    /// Check if a credential is a super admin credential
    /// </summary>
    public static bool IsSuperAdmin(TrustedApiCredential? credential) => credential?.IsSuperAdmin == true;
}
