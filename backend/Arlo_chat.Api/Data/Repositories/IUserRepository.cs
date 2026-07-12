using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> SearchByPrefixAsync(string query, int excludeUserId, int lastRecentUserId, int pageSize);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
