namespace Arlo_chat.Api.Data.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }

    public int OwnerId { get; set; }
    public int TargetId { get; set; }

    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public string? Format { get; set; }

    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
