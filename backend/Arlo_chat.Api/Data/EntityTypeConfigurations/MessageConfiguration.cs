using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arlo_chat.Api.Data.EntityTypeConfigurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> entity)
    {
        entity.Property(m => m.Content).IsRequired();
        entity.Property(m => m.Format).HasMaxLength(64);

        entity.HasIndex(m => m.ConversationId);

        entity.HasOne<Conversation>()
              .WithMany()
              .HasForeignKey(m => m.ConversationId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
