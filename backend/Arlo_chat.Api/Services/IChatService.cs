using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Services;

public interface IChatService
{
    Task<bool> IsValidConnectionAsync(int userId, int targetId);

    Task<List<Conversation>> GetUserConversationsAsync(int userId, int lastRecentConversationId, int pageSize);
    Task<List<int>> GetUserConversationIdsAsync(int userId);

    Task<List<Message>?> GetConversationMessagesAsync(int conversationId, int currentUserId, int lastRecentMessageId, int pageSize);

    Task<(Conversation Conversation, bool WasCreated)> GetOrCreateSingleConversationAsync(int currentUserId, int targetUserId);
    Task<Conversation> CreateGroupConversationAsync(int currentUserId, string? name, IEnumerable<int> participantUserIds);
    Task<Conversation?> GetGroupConversationForMemberAsync(int conversationId, int userId);

    Task<Message> SendMessageAsync(Message message);
    Task SetLatestMessageAsync(Conversation conversation, int messageId);
}
