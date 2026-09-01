using System.Security.Claims;

namespace OnePage.Platform;

/// <summary>
/// User credentials for authentication. Tenant is optional: omit it to auto-select
/// a single organization or receive the list when the user belongs to more than one.
/// </summary>
public sealed record LoginRequest(string Username, string Password, string? TenantId = null);

/// <summary>
/// Lookup organizations a user belongs to (by email/username).
/// </summary>
public sealed record OrganizationLookupRequest(string Email);

/// <summary>
/// An organization the authenticated user may sign in to.
/// </summary>
public sealed record OrganizationOption(string TenantId, string TenantName, string UserId);

/// <summary>
/// Refresh token request
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Authentication response with tokens
/// </summary>
public sealed record AuthenticationResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string UserId,
    string TenantId,
    string[] Roles,
    string[] Permissions);

/// <summary>
/// User principal information
/// </summary>
public sealed record UserPrincipal(
    string Id,
    string Username,
    string? Email,
    string[] Roles,
    string[] Permissions,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    string? TenantId = null);

/// <summary>
/// Token configuration
/// </summary>
public sealed record TokenConfiguration(
    string Issuer,
    string Audience,
    string SecretKey,
    int AccessTokenExpirationMinutes = 60,
    int RefreshTokenExpirationDays = 7);

/// <summary>
/// Password hash configuration
/// </summary>
public sealed record PasswordHashConfiguration(
    int WorkFactor = 12,
    int SaltSize = 16,
    int HashSize = 20,
    int Iterations = 1);

/// <summary>
/// Authentication options
/// </summary>
public sealed record AuthenticationOptions(
    TokenConfiguration Token,
    PasswordHashConfiguration Password,
    bool AllowApiKeyAuthentication = true,
    bool AllowJwtAuthentication = true,
    bool RequireMultiFactorAuthentication = false);

/// <summary>
/// Authentication failure types
/// </summary>
public enum AuthenticationFailureReason
{
    InvalidCredentials,
    UserNotFound,
    UserInactive,
    TenantNotFound,
    InvalidToken,
    TokenExpired,
    InvalidApiKey,
    AccessDenied
}

/// <summary>
/// Authentication result
/// </summary>
public sealed record AuthenticationResult(
    bool Success,
    AuthenticationFailureReason? FailureReason,
    string? ErrorMessage,
    UserPrincipal? Principal,
    AuthenticationResponse? Tokens,
    bool RequiresOrganizationSelection = false,
    IReadOnlyList<OrganizationOption>? Organizations = null);
