namespace Arlo_chat.Api.Models;

public record FriendRequestDto(int FriendshipId, int RequesterId, int RequesteeId, string Username, string Email, DateTime CreatedDate);
