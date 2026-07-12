using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Data.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;

    public ConversationRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Conversation?> GetSingleConversationAsync(int userIdLow, int userIdHigh) =>
        _db.Conversations.FirstOrDefaultAsync(c =>
            c.Type == ConversationType.Single && c.UserIdLow == userIdLow && c.UserIdHigh == userIdHigh);

    public Task<Conversation?> GetByIdAsync(int id) =>
        _db.Conversations
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<ConversationParticipant?> GetParticipantAsync(int conversationId, int userId) =>
        _db.ConversationParticipants.FirstOrDefaultAsync(p =>
            p.ConversationId == conversationId && p.UserId == userId && p.LeftAt == null);

    public async Task<bool> IsParticipantAsync(int conversationId, int userId) =>
        await GetParticipantAsync(conversationId, userId) is not null;

    public Task<List<int>> GetUserConversationIdsAsync(int userId) =>
        _db.ConversationParticipants
            .Where(p => p.UserId == userId && p.LeftAt == null)
            .Select(p => p.ConversationId)
            .ToListAsync();

    public async Task<List<Conversation>> GetUserConversationsPageAsync(int userId, int lastRecentConversationId, int pageSize)
    {
        var conversationIds = _db.ConversationParticipants
            .Where(p => p.UserId == userId && p.LeftAt == null)
            .Select(p => p.ConversationId);

        var query = _db.Conversations.Where(c => conversationIds.Contains(c.Id));

        if (lastRecentConversationId != -1)
        {
            var cursor = await _db.Conversations
                .Where(c => c.Id == lastRecentConversationId)
                .Select(c => new { Recency = c.ModifiedDate ?? c.CreatedDate })
                .FirstOrDefaultAsync();

            if (cursor is not null)
            {
                query = query.Where(c =>
                    (c.ModifiedDate ?? c.CreatedDate) < cursor.Recency ||
                    ((c.ModifiedDate ?? c.CreatedDate) == cursor.Recency && c.Id < lastRecentConversationId));
            }
        }

        return await query
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.LatestMessage)
            .OrderByDescending(c => c.ModifiedDate ?? c.CreatedDate)
            .ThenByDescending(c => c.Id)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<bool> UserHasGroupWithNameAsync(int userId, string name)
    {
        var lowerName = name.ToLower();
        return _db.ConversationParticipants
            .Where(p => p.UserId == userId && p.LeftAt == null)
            .Join(_db.Conversations, p => p.ConversationId, c => c.Id, (p, c) => c)
            .AnyAsync(c => c.Type == ConversationType.Group && c.Name != null && c.Name.ToLower() == lowerName);
    }

    public async Task AddAsync(Conversation conversation) =>
        await _db.Conversations.AddAsync(conversation);

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}
