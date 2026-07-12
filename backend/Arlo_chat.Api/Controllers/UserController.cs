using Arlo_chat.Api.Hubs;
using Arlo_chat.Api.Models;
using Arlo_chat.Api.Security;
using Arlo_chat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Arlo_chat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : AuthorizedApiController
{
    private readonly IUserService _userService;
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public UserController(IUserService userService, IHubContext<ChatHub, IChatClient> hubContext)
    {
        _userService = userService;
        _hubContext = hubContext;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<FriendUserDto>>> SearchUsers(
        [FromQuery] string searchQuery, [FromQuery] int lastRecentUserId = -1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _userService.SearchUsersAsync(searchQuery, CurrentUserId, lastRecentUserId, pageSize));
    }

    [ValidateCsrf]
    [HttpPost("friends/{targetUserId:int}")]
    public async Task<ActionResult<ResponseModel>> AddFriend(int targetUserId)
    {
        var response = await _userService.AddFriendAsync(CurrentUserId, targetUserId);
        if (response.Success)
            await _hubContext.Clients.User(targetUserId.ToString()).FriendsChanged();

        return response.Success ? Ok(response) : Conflict(response);
    }

    [ValidateCsrf]
    [HttpPost("friends/manage")]
    public async Task<ActionResult<ResponseModel>> ManageFriend(ManageFriendRequestModel request)
    {
        var response = await _userService.ManageFriendAsync(CurrentUserId, request.TargetUserId, request.Status);
        if (response.Success)
            await _hubContext.Clients.User(request.TargetUserId.ToString()).FriendsChanged();

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("friends")]
    public async Task<ActionResult<List<FriendUserDto>>> FetchFriends(
        [FromQuery] int lastRecentUserId = -1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _userService.FetchFriendsAsync(CurrentUserId, lastRecentUserId, pageSize));
    }

    [HttpGet("friends/requests")]
    public async Task<ActionResult<List<FriendRequestDto>>> FriendRequests(
        [FromQuery] int lastRecentFriendshipId = -1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _userService.FetchFriendRequestsAsync(CurrentUserId, lastRecentFriendshipId, pageSize));
    }
}
