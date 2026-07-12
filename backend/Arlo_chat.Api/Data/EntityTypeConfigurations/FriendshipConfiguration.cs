using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arlo_chat.Api.Data.EntityTypeConfigurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> entity)
    {
        entity.HasIndex(f => new { f.UserIdLow, f.UserIdHigh }).IsUnique();

        entity.HasOne(f => f.Requester)
              .WithMany()
              .HasForeignKey(f => f.RequesterId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(f => f.Requestee)
              .WithMany()
              .HasForeignKey(f => f.RequesteeId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}
