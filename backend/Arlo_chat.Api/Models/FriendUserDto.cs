namespace Arlo_chat.Api.Models;

public record FriendUserDto(int Id, string Username, string Email, UiFriendRequestStatus FriendshipStatus, DateTime LastActiveAt);
