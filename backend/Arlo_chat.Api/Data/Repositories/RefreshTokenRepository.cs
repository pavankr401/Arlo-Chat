using Arlo_chat.Api.Data;
using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Data.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token) =>
        await _db.RefreshTokens.AddAsync(token);

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) =>
        _db.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

    public Task<List<RefreshToken>> GetValidByFamilyIdAsync(Guid familyId) =>
        _db.RefreshTokens.Where(rt => rt.FamilyId == familyId && rt.RevokedAt == null).ToListAsync();

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}
