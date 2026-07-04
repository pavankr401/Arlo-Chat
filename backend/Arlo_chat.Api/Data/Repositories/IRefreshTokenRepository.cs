using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Data.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<List<RefreshToken>> GetValidByFamilyIdAsync(Guid familyId);
    Task SaveChangesAsync();
}
