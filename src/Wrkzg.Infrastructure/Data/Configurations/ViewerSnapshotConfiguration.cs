using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class ViewerSnapshotConfiguration : IEntityTypeConfiguration<ViewerSnapshot>
{
    public void Configure(EntityTypeBuilder<ViewerSnapshot> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => v.StreamSessionId);
    }
}
