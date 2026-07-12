using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Arlo_chat.Api.Data.Entities;

public class ConversationParticipant
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [JsonIgnore]
    public User User { get; set; } = null!;

    [NotMapped]
    public string Username => User?.Username ?? string.Empty;

    public int ConversationId { get; set; }

    [JsonIgnore]
    public Conversation Conversation { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public ParticipantRole Role { get; set; }
}
