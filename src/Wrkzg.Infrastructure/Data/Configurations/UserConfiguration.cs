using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.TwitchId).IsUnique();
        builder.HasIndex(u => u.Username);

        builder.Property(u => u.TwitchId).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
    }
}
