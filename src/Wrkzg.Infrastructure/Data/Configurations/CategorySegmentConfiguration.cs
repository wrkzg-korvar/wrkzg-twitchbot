using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="CategorySegment"/> model.
/// </summary>
public class CategorySegmentConfiguration : IEntityTypeConfiguration<CategorySegment>
{
    /// <summary>Configures the schema for the CategorySegments table.</summary>
    public void Configure(EntityTypeBuilder<CategorySegment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TwitchCategoryId).HasMaxLength(50);
        builder.Property(c => c.CategoryName).IsRequired().HasMaxLength(200);
    }
}
