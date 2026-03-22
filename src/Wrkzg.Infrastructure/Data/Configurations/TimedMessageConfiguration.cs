using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class TimedMessageConfiguration : IEntityTypeConfiguration<TimedMessage>
{
    public void Configure(EntityTypeBuilder<TimedMessage> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.Property(t => t.Messages)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
                     ?? Array.Empty<string>())
            .HasColumnType("TEXT");

        builder.Property(t => t.IntervalMinutes).HasDefaultValue(10);
        builder.Property(t => t.MinChatLines).HasDefaultValue(5);
        builder.Property(t => t.IsEnabled).HasDefaultValue(true);
        builder.Property(t => t.RunWhenOnline).HasDefaultValue(true);
        builder.Property(t => t.RunWhenOffline).HasDefaultValue(false);
    }
}
