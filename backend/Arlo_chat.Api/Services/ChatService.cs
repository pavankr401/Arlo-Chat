using Arlo_chat.Api.Data;
using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Services;

public class ChatService : IChatService
{
    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IFriendshipRepository _friendships;

    public ChatService(IConversationRepository conversations, IMessageRepository messages, IFriendshipRepository friendships)
    {
        _conversations = conversations;
        _messages = messages;
        _friendships = friendships;
    }

    public async Task<bool> IsValidConnectionAsync(int userId, int targetId)
    {
        var low = Math.Min(userId, targetId);
        var high = Math.Max(userId, targetId);
        var friendship = await _friendships.GetByUserPairAsync(low, high);
        return friendship is not null && friendship.Status == FriendRequestStatus.Accepted;
    }

    public Task<List<Conversation>> GetUserConversationsAsync(int userId, int lastRecentConversationId, int pageSize) =>
        _conversations.GetUserConversationsPageAsync(userId, lastRecentConversationId, pageSize);

    public Task<List<int>> GetUserConversationIdsAsync(int userId) =>
        _conversations.GetUserConversationIdsAsync(userId);

    public async Task<List<Message>?> GetConversationMessagesAsync(int conversationId, int currentUserId, int lastRecentMessageId, int pageSize)
    {
        if (!await _conversations.IsParticipantAsync(conversationId, currentUserId))
            return null;

        return await _messages.GetConversationMessagesPageAsync(conversationId, lastRecentMessageId, pageSize);
    }

    public async Task<(Conversation Conversation, bool WasCreated)> GetOrCreateSingleConversationAsync(int currentUserId, int targetUserId)
    {
        var low = Math.Min(currentUserId, targetUserId);
        var high = Math.Max(currentUserId, targetUserId);

        var existing = await _conversations.GetSingleConversationAsync(low, high);
        if (existing is not null)
            return (existing, false);

        var conversation = new Conversation
        {
            Type = ConversationType.Single,
            CreatedBy = currentUserId,
            CreatedDate = DateTime.UtcNow,
            UserIdLow = low,
            UserIdHigh = high
        };
        conversation.Participants.Add(new ConversationParticipant { UserId = currentUserId, Role = ParticipantRole.Member, JoinedAt = DateTime.UtcNow });
        conversation.Participants.Add(new ConversationParticipant { UserId = targetUserId, Role = ParticipantRole.Member, JoinedAt = DateTime.UtcNow });

        try
        {
            await _conversations.AddAsync(conversation);
            await _conversations.SaveChangesAsync();
            return (conversation, true);
        }
        catch (DbUpdateException ex) when (PostgresErrorHelper.IsUniqueViolation(ex))
        {
            var winner = await _conversations.GetSingleConversationAsync(low, high)
                ?? throw new InvalidOperationException("Unique violation on conversation creation but no row found on re-fetch.");
            return (winner, false);
        }
    }

    public async Task<Conversation> CreateGroupConversationAsync(int currentUserId, string? name, IEnumerable<int> participantUserIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Group name is required.");

        if (await _conversations.UserHasGroupWithNameAsync(currentUserId, name))
            throw new InvalidOperationException("You're already in a group with that name.");

        var friendIds = await _friendships.GetAcceptedFriendIdsAsync(currentUserId);
        var seen = new HashSet<int> { currentUserId };
        var participants = new List<ConversationParticipant>
        {
            new() { UserId = currentUserId, Role = ParticipantRole.Admin, JoinedAt = DateTime.UtcNow }
        };

        foreach (var participantId in participantUserIds)
        {
            if (participantId == currentUserId)
                continue;

            if (!friendIds.Contains(participantId))
                throw new InvalidOperationException("Invalid participant in the conversation.");

            if (seen.Add(participantId))
                participants.Add(new ConversationParticipant { UserId = participantId, Role = ParticipantRole.Member, JoinedAt = DateTime.UtcNow });
        }

        if (participants.Count < 3)
            throw new InvalidOperationException("A group needs at least 3 members.");

        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Name = name,
            CreatedBy = currentUserId,
            CreatedDate = DateTime.UtcNow
        };
        foreach (var participant in participants)
            conversation.Participants.Add(participant);

        await _conversations.AddAsync(conversation);
        await _conversations.SaveChangesAsync();

        return await _conversations.GetByIdAsync(conversation.Id) ?? conversation;
    }

    public async Task<Conversation?> GetGroupConversationForMemberAsync(int conversationId, int userId)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId);
        if (conversation is null || conversation.Type != ConversationType.Group)
            return null;

        return conversation.Participants.Any(p => p.UserId == userId && p.LeftAt == null) ? conversation : null;
    }

    public async Task<Message> SendMessageAsync(Message message)
    {
        message.CreatedDate = DateTime.UtcNow;
        await _messages.AddAsync(message);
        await _messages.SaveChangesAsync();
        return message;
    }

    public async Task SetLatestMessageAsync(Conversation conversation, int messageId)
    {
        conversation.LatestMessageId = messageId;
        conversation.ModifiedDate = DateTime.UtcNow;
        await _conversations.SaveChangesAsync();
    }
}
