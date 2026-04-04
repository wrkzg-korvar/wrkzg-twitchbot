using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Poll"/> model.
/// Configures JSON value conversion for the Options array stored as TEXT in SQLite.
/// </summary>
public class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    /// <summary>Configures the schema for the Polls table.</summary>
    public void Configure(EntityTypeBuilder<Poll> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Question).IsRequired().HasMaxLength(200);

        // Store string[] Options as JSON text in SQLite
        builder.Property(p => p.Options)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
                     ?? Array.Empty<string>())
            .HasColumnType("TEXT")
            .Metadata.SetValueComparer(CommandConfiguration.StringArrayComparer());

        builder.Property(p => p.Source)
            .HasConversion<int>();

        builder.Property(p => p.DurationSeconds).HasDefaultValue(60);
        builder.Property(p => p.TwitchPollId).HasMaxLength(100);
        builder.Property(p => p.CreatedBy).HasMaxLength(100).HasDefaultValue("");
        builder.Property(p => p.EndReason).HasConversion<int>().HasDefaultValue(PollEndReason.NotEnded);

        builder.HasMany(p => p.Votes)
            .WithOne(v => v.Poll)
            .HasForeignKey(v => v.PollId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core entity type configuration for the <see cref="PollVote"/> model.
/// Enforces one vote per user per poll via a unique composite index.
/// </summary>
public class PollVoteConfiguration : IEntityTypeConfiguration<PollVote>
{
    /// <summary>Configures the schema for the PollVotes table.</summary>
    public void Configure(EntityTypeBuilder<PollVote> builder)
    {
        builder.HasKey(v => v.Id);

        // One vote per user per poll
        builder.HasIndex(v => new { v.PollId, v.UserId }).IsUnique();

        builder.HasOne(v => v.User)
            .WithMany(u => u.PollVotes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
