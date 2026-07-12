using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Data.Repositories;

public interface IMessageRepository
{
    Task<List<Message>> GetConversationMessagesPageAsync(int conversationId, int lastRecentMessageId, int pageSize);
    Task AddAsync(Message message);
    Task SaveChangesAsync();
}
