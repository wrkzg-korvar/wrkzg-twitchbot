using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="ViewerSnapshot"/> model.
/// </summary>
public class ViewerSnapshotConfiguration : IEntityTypeConfiguration<ViewerSnapshot>
{
    /// <summary>Configures the schema for the ViewerSnapshots table.</summary>
    public void Configure(EntityTypeBuilder<ViewerSnapshot> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => v.StreamSessionId);
    }
}
