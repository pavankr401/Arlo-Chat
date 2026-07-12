using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arlo_chat.Api.Data.EntityTypeConfigurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> entity)
    {
        entity.Property(c => c.Name).HasMaxLength(128);

        entity.HasIndex(c => new { c.UserIdLow, c.UserIdHigh })
              .IsUnique()
              .HasFilter("\"Type\" = 0");

        entity.HasOne(c => c.LatestMessage)
              .WithMany()
              .HasForeignKey(c => c.LatestMessageId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
