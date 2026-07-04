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

    public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email) =>
        _db.Users.AnyAsync(u => u.Username == username || u.Email == email);

    public async Task AddAsync(User user) =>
        await _db.Users.AddAsync(user);

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}
