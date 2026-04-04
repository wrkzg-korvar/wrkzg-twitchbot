using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="ChannelPointReward"/> model.
/// </summary>
public class ChannelPointRewardConfiguration : IEntityTypeConfiguration<ChannelPointReward>
{
    /// <summary>Configures the schema for the ChannelPointRewards table.</summary>
    public void Configure(EntityTypeBuilder<ChannelPointReward> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TwitchRewardId).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.TwitchRewardId).IsUnique();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.ActionPayload).HasMaxLength(500);
    }
}
