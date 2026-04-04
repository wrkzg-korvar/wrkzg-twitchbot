using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="StreamSession"/> model.
/// Configures cascade delete for category segments and viewer snapshots.
/// </summary>
public class StreamSessionConfiguration : IEntityTypeConfiguration<StreamSession>
{
    /// <summary>Configures the schema for the StreamSessions table.</summary>
    public void Configure(EntityTypeBuilder<StreamSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TwitchStreamId).HasMaxLength(100);
        builder.Property(s => s.Title).HasMaxLength(200);
        builder.HasMany(s => s.CategorySegments)
            .WithOne(c => c.StreamSession)
            .HasForeignKey(c => c.StreamSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.ViewerSnapshots)
            .WithOne()
            .HasForeignKey(v => v.StreamSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
