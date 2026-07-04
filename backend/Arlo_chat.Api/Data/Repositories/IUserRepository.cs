using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
