using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="TimedMessage"/> model.
/// Configures JSON value conversion for the Messages array stored as TEXT in SQLite.
/// </summary>
public class TimedMessageConfiguration : IEntityTypeConfiguration<TimedMessage>
{
    /// <summary>Configures the schema for the TimedMessages table.</summary>
    public void Configure(EntityTypeBuilder<TimedMessage> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.Property(t => t.Messages)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
                     ?? Array.Empty<string>())
            .HasColumnType("TEXT")
            .Metadata.SetValueComparer(CommandConfiguration.StringArrayComparer());

        builder.Property(t => t.IntervalMinutes).HasDefaultValue(10);
        builder.Property(t => t.MinChatLines).HasDefaultValue(5);
        builder.Property(t => t.IsEnabled).HasDefaultValue(true);
        builder.Property(t => t.RunWhenOnline).HasDefaultValue(true);
        builder.Property(t => t.RunWhenOffline).HasDefaultValue(false);
        builder.Property(t => t.IsAnnouncement).HasDefaultValue(false);
    }
}
