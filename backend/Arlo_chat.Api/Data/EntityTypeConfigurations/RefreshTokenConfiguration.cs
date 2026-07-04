using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arlo_chat.Api.Data.EntityTypeConfigurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.Property(rt => rt.TokenHash).HasMaxLength(64).IsRequired();

        entity.HasIndex(rt => rt.TokenHash).IsUnique();
        entity.HasIndex(rt => rt.FamilyId);
        entity.HasIndex(rt => rt.UserId);

        entity.HasOne(rt => rt.User)
              .WithMany(u => u.RefreshTokens)
              .HasForeignKey(rt => rt.UserId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
