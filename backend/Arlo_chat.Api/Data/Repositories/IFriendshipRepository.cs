using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Data.Repositories;

public record FriendListItem(int FriendshipId, int UserId, string Username, string Email, DateTime LastActiveAt);

public record PendingFriendRequestItem(int FriendshipId, int RequesterId, int RequesteeId, string Username, string Email, DateTime CreatedDate);

public interface IFriendshipRepository
{
    Task<Friendship?> GetByUserPairAsync(int userIdLow, int userIdHigh);
    Task<HashSet<int>> GetAcceptedFriendIdsAsync(int userId);
    Task<List<FriendListItem>> GetAcceptedFriendsPageAsync(int userId, int lastRecentUserId, int pageSize);
    Task<List<PendingFriendRequestItem>> GetPendingRequestsPageAsync(int userId, int lastRecentFriendshipId, int pageSize);
    Task AddAsync(Friendship friendship);
    Task SaveChangesAsync();
}
