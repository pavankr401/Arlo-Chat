using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(int senderId, Message message);
    Task ConversationCreated(Conversation conversation);
    Task ConversationUpdated(Conversation conversation);
    Task ReceiveGroupMessage(int conversationId, Message message);
    Task FriendsChanged();
}
