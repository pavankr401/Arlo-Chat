namespace Arlo_chat.Api.Data.Entities;

public class Conversation
{
    public int Id { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public ConversationType Type { get; set; }

    public int? LatestMessageId { get; set; }
    public Message? LatestMessage { get; set; }

    public int? UserIdLow { get; set; }
    public int? UserIdHigh { get; set; }

    public string? Name { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
}
