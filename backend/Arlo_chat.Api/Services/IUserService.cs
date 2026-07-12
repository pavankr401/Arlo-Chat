using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Models;

namespace Arlo_chat.Api.Services;

public record RegisterOutcome(bool Success, bool Conflict, string? Message);

public interface IUserService
{
    Task<RegisterOutcome> RegisterAsync(RegisterRequestModel request);
    Task<User?> ValidateCredentialsAsync(string username, string password);
    Task<User?> GetByIdAsync(int id);
    Task TouchLastActiveAsync(int userId);

    Task<List<FriendUserDto>> SearchUsersAsync(string searchQuery, int currentUserId, int lastRecentUserId, int pageSize);
    Task<ResponseModel> AddFriendAsync(int requesterId, int requesteeId);
    Task<ResponseModel> ManageFriendAsync(int performerUserId, int targetUserId, FriendRequestStatus status);
    Task<List<FriendUserDto>> FetchFriendsAsync(int userId, int lastRecentUserId, int pageSize);
    Task<List<FriendRequestDto>> FetchFriendRequestsAsync(int userId, int lastRecentFriendshipId, int pageSize);
}
