namespace Arlo_chat.Api.Data.Entities;

public class Friendship
{
    public int Id { get; set; }

    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public int RequesteeId { get; set; }
    public User Requestee { get; set; } = null!;

    public int UserIdLow { get; set; }
    public int UserIdHigh { get; set; }

    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
}
