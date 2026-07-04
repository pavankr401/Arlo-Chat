using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Security;
using Arlo_chat.Api.Data.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Arlo_chat.Api.Services;

public class TokenService : ITokenService
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly JwtOptions _jwtOptions;

    public TokenService(IRefreshTokenRepository refreshTokens, IOptions<JwtOptions> jwtOptions)
    {
        _refreshTokens = refreshTokens;
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateAccessToken(User user, Guid familyId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fid", familyId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<IssuedRefreshToken> IssueRefreshTokenAsync(int userId, Guid? existingFamilyId)
    {
        var familyId = existingFamilyId ?? Guid.NewGuid();
        var rawToken = GenerateSecureRandomToken();
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = userId,
            FamilyId = familyId,
            TokenHash = Hash(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        });
        await _refreshTokens.SaveChangesAsync();

        return new IssuedRefreshToken(rawToken, familyId, expiresAt);
    }

    public async Task<RefreshRotationResult> ValidateAndRotateRefreshTokenAsync(string rawToken)
    {
        var existing = await _refreshTokens.GetByTokenHashAsync(Hash(rawToken));

        if (existing is null)
            return new RefreshRotationResult(false, null, Guid.Empty, null, default, "Invalid refresh token.");

        if (existing.RevokedAt is not null)
        {
            // Reappearance of an already-rotated-out token: treat as theft and burn the whole family.
            await RevokeFamilyAsync(existing.FamilyId);
            return new RefreshRotationResult(false, null, existing.FamilyId, null, default, "Refresh token reuse detected.");
        }

        if (existing.ExpiresAt <= DateTime.UtcNow)
            return new RefreshRotationResult(false, null, existing.FamilyId, null, default, "Refresh token expired.");

        existing.RevokedAt = DateTime.UtcNow;

        // existing (update) and the new row (insert) share this scoped DbContext, so the SaveChanges
        // inside IssueRefreshTokenAsync commits both the revoke and the new issuance atomically.
        var issued = await IssueRefreshTokenAsync(existing.UserId, existing.FamilyId);

        return new RefreshRotationResult(true, existing.User, issued.FamilyId, issued.RawToken, issued.ExpiresAt, null);
    }

    public async Task RevokeFamilyAsync(Guid familyId)
    {
        var rows = await _refreshTokens.GetValidByFamilyIdAsync(familyId);
        foreach (var row in rows)
            row.RevokedAt = DateTime.UtcNow;

        await _refreshTokens.SaveChangesAsync();
    }

    public string GenerateCsrfToken() => GenerateSecureRandomToken();

    private static string GenerateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
