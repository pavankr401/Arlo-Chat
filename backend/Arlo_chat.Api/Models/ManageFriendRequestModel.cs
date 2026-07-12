using Arlo_chat.Api.Data.Entities;

namespace Arlo_chat.Api.Models;

public record ManageFriendRequestModel(int TargetUserId, FriendRequestStatus Status);
