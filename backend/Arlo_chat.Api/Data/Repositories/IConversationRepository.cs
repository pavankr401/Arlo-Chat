using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Data.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetSingleConversationAsync(int userIdLow, int userIdHigh);
    Task<Conversation?> GetByIdAsync(int id);
    Task<ConversationParticipant?> GetParticipantAsync(int conversationId, int userId);
    Task<bool> IsParticipantAsync(int conversationId, int userId);
    Task<List<int>> GetUserConversationIdsAsync(int userId);
    Task<List<Conversation>> GetUserConversationsPageAsync(int userId, int lastRecentConversationId, int pageSize);
    Task<bool> UserHasGroupWithNameAsync(int userId, string name);
    Task AddAsync(Conversation conversation);
    Task SaveChangesAsync();
}
