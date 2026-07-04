using Arlo_chat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arlo_chat.Api.Data.EntityTypeConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.Property(u => u.Username).HasMaxLength(32).IsRequired();
        entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
        entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();

        entity.HasIndex(u => u.Username).IsUnique();
        entity.HasIndex(u => u.Email).IsUnique();
    }
}
