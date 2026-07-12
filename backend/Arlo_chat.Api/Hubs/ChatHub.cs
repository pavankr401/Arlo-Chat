using System.IdentityModel.Tokens.Jwt;
using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Arlo_chat.Api.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly PresenceTracker _presenceTracker;

    public ChatHub(IChatService chatService, IUserService userService, PresenceTracker presenceTracker)
    {
        _chatService = chatService;
        _userService = userService;
        _presenceTracker = presenceTracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;
        _presenceTracker.AddConnection(userId.ToString(), Context.ConnectionId);

        await _userService.TouchLastActiveAsync(userId);

        var conversationIds = await _chatService.GetUserConversationIdsAsync(userId);
        foreach (var conversationId in conversationIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _presenceTracker.RemoveConnection(CurrentUserId.ToString(), Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int targetId, Message message)
    {
        var currentUserId = CurrentUserId;
        if (targetId == currentUserId)
            throw new HubException("Sending messages to yourself is not allowed.");

        if (!await _chatService.IsValidConnectionAsync(currentUserId, targetId))
            throw new HubException("Friendship doesn't exist - can't send a message.");

        var (conversation, wasCreated) = await _chatService.GetOrCreateSingleConversationAsync(currentUserId, targetId);

        message.OwnerId = currentUserId;
        message.TargetId = targetId;
        message.ConversationId = conversation.Id;
        message.CreatedBy = currentUserId;

        var savedMessage = await _chatService.SendMessageAsync(message);
        await _chatService.SetLatestMessageAsync(conversation, savedMessage.Id);

        if (wasCreated)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversation.Id.ToString());
            foreach (var connectionId in _presenceTracker.GetConnections(targetId.ToString()))
                await Groups.AddToGroupAsync(connectionId, conversation.Id.ToString());

            await Clients.User(targetId.ToString()).ConversationCreated(conversation);
            await Clients.User(currentUserId.ToString()).ConversationCreated(conversation);
        }
        else
        {
            await Clients.User(targetId.ToString()).ConversationUpdated(conversation);
            await Clients.User(currentUserId.ToString()).ConversationUpdated(conversation);
        }

        await Clients.User(targetId.ToString()).ReceiveMessage(currentUserId, savedMessage);
        await Clients.User(currentUserId.ToString()).ReceiveMessage(currentUserId, savedMessage);
    }

    public async Task CreateGroupConversation(string name, List<int> participantUserIds)
    {
        var currentUserId = CurrentUserId;
        Conversation conversation;

        try
        {
            conversation = await _chatService.CreateGroupConversationAsync(currentUserId, name, participantUserIds);
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(ex.Message);
        }

        foreach (var participant in conversation.Participants)
        {
            await Clients.User(participant.UserId.ToString()).ConversationCreated(conversation);
            foreach (var connectionId in _presenceTracker.GetConnections(participant.UserId.ToString()))
                await Groups.AddToGroupAsync(connectionId, conversation.Id.ToString());
        }
    }

    public async Task SendGroupMessage(int conversationId, Message message)
    {
        var currentUserId = CurrentUserId;
        var conversation = await _chatService.GetGroupConversationForMemberAsync(conversationId, currentUserId);
        if (conversation is null)
            throw new HubException("Invalid conversation.");

        message.ConversationId = conversation.Id;
        message.OwnerId = currentUserId;
        message.TargetId = conversation.Id;
        message.CreatedBy = currentUserId;

        var savedMessage = await _chatService.SendMessageAsync(message);
        await _chatService.SetLatestMessageAsync(conversation, savedMessage.Id);

        await Clients.Group(conversationId.ToString()).ConversationUpdated(conversation);
        await Clients.Group(conversationId.ToString()).ReceiveGroupMessage(conversationId, savedMessage);
    }

    private int CurrentUserId =>
        int.Parse(Context.User!.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
}
