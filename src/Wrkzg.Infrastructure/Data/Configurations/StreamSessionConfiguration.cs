using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class StreamSessionConfiguration : IEntityTypeConfiguration<StreamSession>
{
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
