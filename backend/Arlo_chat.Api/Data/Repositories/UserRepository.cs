using Arlo_chat.Api.Data;
using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string username) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<List<User>> SearchByPrefixAsync(string query, int excludeUserId, int lastRecentUserId, int pageSize)
    {
        var lowerQuery = query.ToLower();
        var dbQuery = _db.Users.Where(u =>
            u.Id != excludeUserId &&
            (u.Username.ToLower().StartsWith(lowerQuery) || u.Email.ToLower().StartsWith(lowerQuery)));

        if (lastRecentUserId != -1)
            dbQuery = dbQuery.Where(u => u.Id > lastRecentUserId);

        return dbQuery.OrderBy(u => u.Id).Take(pageSize).ToListAsync();
    }

    public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email) =>
        _db.Users.AnyAsync(u => u.Username == username || u.Email == email);

    public async Task AddAsync(User user) =>
        await _db.Users.AddAsync(user);

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}
