using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Services;

public record IssuedRefreshToken(string RawToken, Guid FamilyId, DateTime ExpiresAt);

public record RefreshRotationResult(bool Success, User? User, Guid FamilyId, string? NewRawToken, DateTime NewExpiresAt, string? FailureReason);

public interface ITokenService
{
    string GenerateAccessToken(User user, Guid familyId);
    Task<IssuedRefreshToken> IssueRefreshTokenAsync(int userId, Guid? existingFamilyId);
    Task<RefreshRotationResult> ValidateAndRotateRefreshTokenAsync(string rawToken);
    Task RevokeFamilyAsync(Guid familyId);
    string GenerateCsrfToken();
}
