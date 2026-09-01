using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace OnePage.Platform;

/// <summary>
/// JWT token service for generating and validating tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate an access token
    /// </summary>
    string GenerateAccessToken(string userId, string tenantId, string[] roles, string[] permissions, DateTimeOffset? expiresAt = null);
    
    /// <summary>
    /// Generate a refresh token
    /// </summary>
    string GenerateRefreshToken(string userId, DateTimeOffset? expiresAt = null);
    
    /// <summary>
    /// Validate an access token
    /// </summary>
    TokenValidationResult ValidateAccessToken(string token);
    
    /// <summary>
    /// Validate a refresh token
    /// </summary>
    TokenValidationResult ValidateRefreshToken(string token);
    
    /// <summary>
    /// Get claims from a token
    /// </summary>
    IReadOnlyList<Claim> GetClaimsFromToken(string token);
    
    /// <summary>
    /// Get user ID from a token
    /// </summary>
    string? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Get tenant ID from a token
    /// </summary>
    string? GetTenantIdFromToken(string token);
}

/// <summary>
/// Token validation result
/// </summary>
public sealed record TokenValidationResult(
    bool IsValid,
    string? UserId,
    string? TenantId,
    DateTimeOffset? ExpiresAt,
    string? ErrorMessage,
    SecurityToken? Token);

/// <summary>
/// JWT token service implementation
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secretKey;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly SymmetricSecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    
    public JwtTokenService(
        string issuer,
        string audience,
        string secretKey,
        int accessTokenExpirationMinutes = 60,
        int refreshTokenExpirationDays = 7)
    {
        _issuer = issuer;
        _audience = audience;
        _secretKey = secretKey;
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
        _refreshTokenExpirationDays = refreshTokenExpirationDays;
        
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = _securityKey,
            ClockSkew = TimeSpan.Zero
        };
        // Preserve original JWT claim types (e.g. "sub") instead of mapping them
        // to .NET claim types (e.g. ClaimTypes.NameIdentifier). This keeps
        // ValidateAccessToken/ValidateRefreshToken claim lookups working.
        _tokenHandler.MapInboundClaims = false;
    }
    
    public string GenerateAccessToken(string userId, string tenantId, string[] roles, string[] permissions, DateTimeOffset? expiresAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = expiresAt ?? now.AddMinutes(_accessTokenExpirationMinutes);
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("tenant_id", tenantId),
            new Claim("user_id", userId)
        };
        
        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        // Add permissions
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }
        
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiry.UtcDateTime,
            signingCredentials: _signingCredentials);
        
        return _tokenHandler.WriteToken(token);
    }
    
    public string GenerateRefreshToken(string userId, DateTimeOffset? expiresAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = expiresAt ?? now.AddDays(_refreshTokenExpirationDays);
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("token_type", "refresh")
        };
        
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiry.UtcDateTime,
            signingCredentials: _signingCredentials);
        
        return _tokenHandler.WriteToken(token);
    }
    
    public TokenValidationResult ValidateAccessToken(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            
            var userId = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var tenantId = principal.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value ?? userId;
            var expiresAt = validatedToken.ValidTo;
            
            return new TokenValidationResult(
                true,
                userId,
                tenantId,
                expiresAt,
                null,
                validatedToken);
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult(false, null, null, null, "Token expired", null);
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult(false, null, null, null, "Invalid token signature", null);
        }
        catch (Exception ex)
        {
            return new TokenValidationResult(false, null, null, null, ex.Message, null);
        }
    }
    
    public TokenValidationResult ValidateRefreshToken(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            
            // Check token type
            var tokenType = principal.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
            if (tokenType != "refresh")
            {
                return new TokenValidationResult(false, null, null, null, "Invalid token type", null);
            }
            
            var userId = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var expiresAt = validatedToken.ValidTo;
            
            return new TokenValidationResult(
                true,
                userId,
                null,
                expiresAt,
                null,
                validatedToken);
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult(false, null, null, null, "Refresh token expired", null);
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult(false, null, null, null, "Invalid refresh token signature", null);
        }
        catch (Exception ex)
        {
            return new TokenValidationResult(false, null, null, null, ex.Message, null);
        }
    }
    
    public IReadOnlyList<Claim> GetClaimsFromToken(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
            return principal.Claims.ToList().AsReadOnly();
        }
        catch
        {
            return Array.Empty<Claim>();
        }
    }
    
    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
            return principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }
    
    public string? GetTenantIdFromToken(string token)
    {
        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
            return principal.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        }
        catch
        {
            return null;
        }
    }
}
