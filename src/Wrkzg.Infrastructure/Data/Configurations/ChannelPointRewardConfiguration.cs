using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class ChannelPointRewardConfiguration : IEntityTypeConfiguration<ChannelPointReward>
{
    public void Configure(EntityTypeBuilder<ChannelPointReward> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TwitchRewardId).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.TwitchRewardId).IsUnique();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.ActionPayload).HasMaxLength(500);
    }
}
