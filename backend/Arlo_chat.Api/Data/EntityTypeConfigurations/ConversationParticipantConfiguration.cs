using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arlo_chat.Api.Data.EntityTypeConfigurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> entity)
    {
        entity.HasIndex(p => new { p.ConversationId, p.UserId }).IsUnique();
        entity.HasIndex(p => p.UserId);

        entity.HasOne(p => p.Conversation)
              .WithMany(c => c.Participants)
              .HasForeignKey(p => p.ConversationId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(p => p.User)
              .WithMany()
              .HasForeignKey(p => p.UserId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
