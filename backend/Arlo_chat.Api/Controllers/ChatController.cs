using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arlo_chat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : AuthorizedApiController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<Conversation>>> GetUserConversations(
        [FromQuery] int lastRecentConversationId = -1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _chatService.GetUserConversationsAsync(CurrentUserId, lastRecentConversationId, pageSize));
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<ActionResult<List<Message>>> GetConversationMessages(
        int conversationId, [FromQuery] int lastRecentMessageId = -1, [FromQuery] int pageSize = 30)
    {
        var messages = await _chatService.GetConversationMessagesAsync(conversationId, CurrentUserId, lastRecentMessageId, pageSize);
        if (messages is null)
            return Forbid();

        return Ok(messages);
    }
}
