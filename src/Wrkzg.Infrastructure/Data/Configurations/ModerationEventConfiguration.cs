using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>EF Core configuration for the ModerationEvent entity.</summary>
public class ModerationEventConfiguration : IEntityTypeConfiguration<ModerationEvent>
{
    /// <summary>Configures the ModerationEvent table schema.</summary>
    public void Configure(EntityTypeBuilder<ModerationEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TwitchUserId);
        builder.HasIndex(e => e.CreatedAt);
        builder.Property(e => e.EventType).HasConversion<int>();
        builder.Property(e => e.TwitchUserId).IsRequired().HasMaxLength(64);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Actor).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Reason).HasMaxLength(1024);
    }
}
