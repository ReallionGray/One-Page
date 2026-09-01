using Microsoft.EntityFrameworkCore;
namespace OnePage.Platform;

/// <summary>
/// Main authentication service
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate a user with username and password
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Authenticate using API key
    /// </summary>
    Task<AuthenticationResult> AuthenticateWithApiKeyAsync(string apiKey, string? tenantId = null, CancellationToken ct = default);
    
    /// <summary>
    /// Validate an access token
    /// </summary>
    Task<AuthenticationResult> ValidateTokenAsync(string token, CancellationToken ct = default);
    
    /// <summary>
    /// Get user principal from user ID
    /// </summary>
    Task<UserPrincipal?> GetUserPrincipalAsync(string userId, string tenantId, CancellationToken ct = default);
}

/// <summary>
/// Authentication service implementation
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHRRepository _hrRepository;
    private readonly IAuthorizationRepository _authRepository;
    private readonly ITrustedApiCredentialResolver _credentialResolver;
    private readonly ITenantRepository _tenantRepository;
    private readonly TokenConfiguration _tokenConfig;
    private readonly OrganizationDbContext _db;

    private static readonly Dictionary<string, string> DemoPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["superadmin@demo.com"] = "SuperAdmin@123!",
        ["admin@demo.com"] = "Admin@123!",
        ["hrmanager@demo.com"] = "HRManager@123!",
        ["accountant@demo.com"] = "Accountant@123!",
        ["sales@demo.com"] = "Sales@123!",
        ["user@demo.com"] = "User@123!",
    };
    
    public AuthenticationService(
        IJwtTokenService tokenService,
        IPasswordHasher passwordHasher,
        IHRRepository hrRepository,
        IAuthorizationRepository authRepository,
        ITrustedApiCredentialResolver credentialResolver,
        ITenantRepository tenantRepository,
        TokenConfiguration tokenConfig,
        OrganizationDbContext db)
    {
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _hrRepository = hrRepository;
        _authRepository = authRepository;
        _credentialResolver = credentialResolver;
        _tenantRepository = tenantRepository;
        _tokenConfig = tokenConfig;
        _db = db;
    }
    
    public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return InvalidCredentials();

        var username = request.Username.Trim();
        var isSuperAdminLogin = string.Equals(username, "superadmin@demo.com", StringComparison.OrdinalIgnoreCase);

        var employees = await _hrRepository.GetByEmailAcrossAllTenantsAsync(username, ct);
        if (employees.Count == 0 && !isSuperAdminLogin)
            return InvalidCredentials();

        // Verify password against DB or seeded credentials
        if (DemoPasswords.TryGetValue(username, out var expectedDemoPassword))
        {
            if (!string.Equals(request.Password, expectedDemoPassword, StringComparison.Ordinal))
                return InvalidCredentials();
        }
        else
        {
            var profile = await _db.UserProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == username, ct);
            if (profile == null)
            {
                if (request.Password.Length == 0)
                    return InvalidCredentials();
            }
            else
            {
                if (string.IsNullOrEmpty(profile.PasswordHash))
                {
                    if (request.Password.Length == 0)
                        return InvalidCredentials();
                }
                else
                {
                    if (!_passwordHasher.VerifyPassword(request.Password, profile.PasswordHash))
                        return InvalidCredentials();
                }
            }
        }

        var organizations = await BuildOrganizationsAsync(employees, isSuperAdminLogin, ct);
        if (organizations.Count == 0)
            return InvalidCredentials();

        if (!string.IsNullOrWhiteSpace(request.TenantId))
        {
            var selected = organizations.FirstOrDefault(o =>
                string.Equals(o.TenantId, request.TenantId, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
                return InvalidCredentials();
            return await CompleteLoginAsync(username, selected, isSuperAdminLogin, ct);
        }

        if (organizations.Count > 1)
        {
            return new AuthenticationResult(
                false, null, null, null, null,
                RequiresOrganizationSelection: true,
                Organizations: organizations);
        }

        return await CompleteLoginAsync(username, organizations[0], isSuperAdminLogin, ct);
    }

    private static AuthenticationResult InvalidCredentials() =>
        new(false, AuthenticationFailureReason.InvalidCredentials, "Invalid credentials", null, null);

    private static bool IsPasswordValid(string username, string password)
    {
        if (DemoPasswords.TryGetValue(username, out var expected))
            return string.Equals(password, expected, StringComparison.Ordinal);
        // Employees created outside the demo seed can sign in with any non-empty password.
        return password.Length > 0;
    }

    private async Task<IReadOnlyList<OrganizationOption>> BuildOrganizationsAsync(
        IReadOnlyList<Employee> employees,
        bool isSuperAdminLogin,
        CancellationToken ct)
    {
        var matches = employees.ToList();
        if (isSuperAdminLogin && matches.Count == 0)
        {
            // Super admin is seeded as a membership even if the employee row is missing.
            matches.Add(new Employee(SuperAdmin.UserId, "demo-tenant", "Super", "Admin", "superadmin@demo.com"));
        }

        var tenantIds = matches.Select(e => e.TenantId).Distinct().ToArray();
        var tenants = await _tenantRepository.ListByIdsAsync(tenantIds, ct);
        var names = tenants.ToDictionary(t => t.Id, t => t.Name, StringComparer.OrdinalIgnoreCase);

        return matches
            .GroupBy(e => e.TenantId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var emp = g.First();
                var name = names.TryGetValue(emp.TenantId, out var tenantName) ? tenantName : emp.TenantId;
                var userId = isSuperAdminLogin ? SuperAdmin.UserId : emp.Id;
                return new OrganizationOption(emp.TenantId, name, userId);
            })
            .ToList();
    }

    private async Task<AuthenticationResult> CompleteLoginAsync(
        string username,
        OrganizationOption organization,
        bool isSuperAdminLogin,
        CancellationToken ct)
    {
        var effectiveTenantId = organization.TenantId;
        var effectiveUserId = isSuperAdminLogin ? SuperAdmin.UserId : organization.UserId;

        var membership = await _authRepository.GetMembershipForUserAsync(effectiveTenantId, effectiveUserId, ct);
        if (membership is null && isSuperAdminLogin && !string.Equals(effectiveUserId, organization.UserId, StringComparison.Ordinal))
            membership = await _authRepository.GetMembershipForUserAsync(effectiveTenantId, organization.UserId, ct);

        if (membership is null || !membership.IsActive)
            return new AuthenticationResult(false, AuthenticationFailureReason.UserInactive, "User is inactive", null, null);

        effectiveUserId = membership.UserId;

        var assignments = await _authRepository.GetAssignmentsAsync(membership, ct);
        var roles = assignments.Select(a => a.Role.Name).Distinct().ToArray();
        var permissions = assignments.SelectMany(a => a.Permissions.Select(p => p.Permission)).Distinct().ToArray();

        if (isSuperAdminLogin)
        {
            effectiveUserId = SuperAdmin.UserId;
            if (!roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase))
                roles = roles.Append("SuperAdmin").ToArray();
        }

        var accessToken = _tokenService.GenerateAccessToken(effectiveUserId, effectiveTenantId, roles, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken(effectiveUserId);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_tokenConfig.AccessTokenExpirationMinutes);

        var principal = new UserPrincipal(
            effectiveUserId,
            username,
            username,
            roles,
            permissions,
            membership.IsActive,
            DateTimeOffset.UtcNow,
            effectiveTenantId);

        var response = new AuthenticationResponse(
            accessToken,
            refreshToken,
            expiresAt,
            effectiveUserId,
            effectiveTenantId,
            roles,
            permissions);

        return new AuthenticationResult(true, null, null, principal, response);
    }
    
    public async Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidToken, "Refresh token is required", null, null);
        
        // Validate refresh token
        var validationResult = _tokenService.ValidateRefreshToken(request.RefreshToken);
        if (!validationResult.IsValid)
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidToken, validationResult.ErrorMessage, null, null);
        
        var userId = validationResult.UserId;
        if (userId is null)
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidToken, "Invalid refresh token", null, null);
        
        // Get user's roles and permissions (need tenant context)
        // For now, we'll use a placeholder - in production, you'd look up the user's tenant
        var membership = await _authRepository.GetMembershipForUserAsync("", userId, ct);
        if (membership is null || !membership.IsActive)
            return new AuthenticationResult(false, AuthenticationFailureReason.UserInactive, "User is inactive", null, null);
        
        var assignments = await _authRepository.GetAssignmentsAsync(membership, ct);
        var roles = assignments.Select(a => a.Role.Name).Distinct().ToArray();
        var permissions = assignments.SelectMany(a => a.Permissions.Select(p => p.Permission)).Distinct().ToArray();
        
        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(userId, membership.TenantId, roles, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken(userId);
        
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_tokenConfig.AccessTokenExpirationMinutes);
        
        var principal = new UserPrincipal(
            userId,
            userId,
            null,
            roles,
            permissions,
            membership.IsActive,
            DateTimeOffset.UtcNow,
            membership.TenantId);
        
        var response = new AuthenticationResponse(
            accessToken,
            refreshToken,
            expiresAt,
            userId,
            membership.TenantId,
            roles,
            permissions);
        
        return new AuthenticationResult(true, null, null, principal, response);
    }
    
    public async Task<AuthenticationResult> AuthenticateWithApiKeyAsync(string apiKey, string? tenantId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidApiKey, "API key is required", null, null);
        
        // Resolve credential
        var credential = _credentialResolver.Resolve(apiKey);
        if (credential is null)
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidApiKey, "Invalid API key", null, null);
        
        // Check tenant access
        if (!credential.AllowedTenantIds.Contains("*") && !credential.AllowedTenantIds.Contains(tenantId ?? ""))
            return new AuthenticationResult(false, AuthenticationFailureReason.AccessDenied, "API key not authorized for this tenant", null, null);
        
        // Get effective tenant
        var effectiveTenantId = tenantId ?? credential.AllowedTenantIds.First();
        
        // For API key auth, we don't have user roles/permissions in the same way
        // So we'll return a minimal principal
        var principal = new UserPrincipal(
            credential.UserId,
            credential.UserId,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            true,
            DateTimeOffset.UtcNow,
            effectiveTenantId);
        
        // API key authentication doesn't return tokens - the API key itself is the credential
        return new AuthenticationResult(true, null, null, principal, null);
    }
    
    public async Task<AuthenticationResult> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidToken, "Token is required", null, null);
        
        var validationResult = _tokenService.ValidateAccessToken(token);
        if (!validationResult.IsValid)
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidToken, validationResult.ErrorMessage, null, null);
        
        var userId = validationResult.UserId;
        var tenantId = validationResult.TenantId;
        
        if (userId is null || tenantId is null)
            return new AuthenticationResult(false, AuthenticationFailureReason.InvalidToken, "Invalid token claims", null, null);
        
        // Get user's roles and permissions
        var membership = await _authRepository.GetMembershipForUserAsync(tenantId, userId, ct);
        if (membership is null || !membership.IsActive)
            return new AuthenticationResult(false, AuthenticationFailureReason.UserInactive, "User is inactive", null, null);
        
        var assignments = await _authRepository.GetAssignmentsAsync(membership, ct);
        var roles = assignments.Select(a => a.Role.Name).Distinct().ToArray();
        var permissions = assignments.SelectMany(a => a.Permissions.Select(p => p.Permission)).Distinct().ToArray();
        
        var principal = new UserPrincipal(
            userId,
            userId,
            null,
            roles,
            permissions,
            membership.IsActive,
            DateTimeOffset.UtcNow,
            tenantId);
        
        return new AuthenticationResult(true, null, null, principal, null);
    }
    
    public async Task<UserPrincipal?> GetUserPrincipalAsync(string userId, string tenantId, CancellationToken ct = default)
    {
        var membership = await _authRepository.GetMembershipForUserAsync(tenantId, userId, ct);
        if (membership is null || !membership.IsActive)
            return null;
        
        var assignments = await _authRepository.GetAssignmentsAsync(membership, ct);
        var roles = assignments.Select(a => a.Role.Name).Distinct().ToArray();
        var permissions = assignments.SelectMany(a => a.Permissions.Select(p => p.Permission)).Distinct().ToArray();
        
        return new UserPrincipal(
            userId,
            userId,
            null,
            roles,
            permissions,
            membership.IsActive,
            DateTimeOffset.UtcNow,
            tenantId);
    }
}
