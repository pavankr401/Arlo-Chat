using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Data.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _db;

    public MessageRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Message>> GetConversationMessagesPageAsync(int conversationId, int lastRecentMessageId, int pageSize)
    {
        var query = _db.Messages.Where(m => m.ConversationId == conversationId);

        if (lastRecentMessageId != -1)
            query = query.Where(m => m.Id < lastRecentMessageId);

        return query.OrderByDescending(m => m.Id).Take(pageSize).ToListAsync();
    }

    public async Task AddAsync(Message message) =>
        await _db.Messages.AddAsync(message);

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}
