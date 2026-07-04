using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Models;

namespace Arlo_chat.Api.Services;

public record RegisterOutcome(bool Success, bool Conflict, string? Message);

public interface IUserService
{
    Task<RegisterOutcome> RegisterAsync(RegisterRequestModel request);
    Task<User?> ValidateCredentialsAsync(string username, string password);
    Task<User?> GetByIdAsync(int id);
}
