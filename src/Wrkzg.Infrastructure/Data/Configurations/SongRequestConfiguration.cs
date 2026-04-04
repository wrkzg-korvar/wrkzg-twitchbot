using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="SongRequest"/> model.
/// </summary>
public class SongRequestConfiguration : IEntityTypeConfiguration<SongRequest>
{
    /// <summary>Configures the schema for the SongRequests table.</summary>
    public void Configure(EntityTypeBuilder<SongRequest> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.VideoId).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(300);
        builder.Property(s => s.ThumbnailUrl).HasMaxLength(500);
        builder.Property(s => s.RequestedBy).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Status);
    }
}
