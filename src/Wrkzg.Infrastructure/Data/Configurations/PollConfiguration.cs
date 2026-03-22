using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Question).IsRequired().HasMaxLength(200);

        // Store string[] Options as JSON text in SQLite
        builder.Property(p => p.Options)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
                     ?? System.Array.Empty<string>())
            .HasColumnType("TEXT");

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

public class PollVoteConfiguration : IEntityTypeConfiguration<PollVote>
{
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
