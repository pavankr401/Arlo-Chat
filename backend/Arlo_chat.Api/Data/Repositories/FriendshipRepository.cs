using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Data.Repositories;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly AppDbContext _db;

    public FriendshipRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Friendship?> GetByUserPairAsync(int userIdLow, int userIdHigh) =>
        _db.Friendships.FirstOrDefaultAsync(f => f.UserIdLow == userIdLow && f.UserIdHigh == userIdHigh);

    public async Task<HashSet<int>> GetAcceptedFriendIdsAsync(int userId)
    {
        var ids = await _db.Friendships
            .Where(f => f.Status == FriendRequestStatus.Accepted && (f.RequesterId == userId || f.RequesteeId == userId))
            .Select(f => f.RequesterId == userId ? f.RequesteeId : f.RequesterId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public Task<List<FriendListItem>> GetAcceptedFriendsPageAsync(int userId, int lastRecentUserId, int pageSize)
    {
        var query = _db.Friendships
            .Where(f => f.Status == FriendRequestStatus.Accepted && (f.RequesterId == userId || f.RequesteeId == userId))
            .Select(f => new { f.Id, FriendId = f.RequesterId == userId ? f.RequesteeId : f.RequesterId });

        if (lastRecentUserId != -1)
            query = query.Where(x => x.FriendId > lastRecentUserId);

        return query
            .OrderBy(x => x.FriendId)
            .Join(_db.Users, x => x.FriendId, u => u.Id, (x, u) => new FriendListItem(x.Id, u.Id, u.Username, u.Email, u.LastActiveAt))
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<List<PendingFriendRequestItem>> GetPendingRequestsPageAsync(int userId, int lastRecentFriendshipId, int pageSize)
    {
        var query = _db.Friendships
            .Where(f => f.Status == FriendRequestStatus.Pending && (f.RequesterId == userId || f.RequesteeId == userId));

        if (lastRecentFriendshipId != -1)
            query = query.Where(f => f.Id > lastRecentFriendshipId);

        return query
            .OrderBy(f => f.Id)
            .Select(f => new { f.Id, f.RequesterId, f.RequesteeId, f.CreatedDate, OtherUserId = f.RequesterId == userId ? f.RequesteeId : f.RequesterId })
            .Join(_db.Users, x => x.OtherUserId, u => u.Id,
                (x, u) => new PendingFriendRequestItem(x.Id, x.RequesterId, x.RequesteeId, u.Username, u.Email, x.CreatedDate))
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(Friendship friendship) =>
        await _db.Friendships.AddAsync(friendship);

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}
