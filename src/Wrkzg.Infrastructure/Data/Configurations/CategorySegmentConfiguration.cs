using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class CategorySegmentConfiguration : IEntityTypeConfiguration<CategorySegment>
{
    public void Configure(EntityTypeBuilder<CategorySegment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TwitchCategoryId).HasMaxLength(50);
        builder.Property(c => c.CategoryName).IsRequired().HasMaxLength(200);
    }
}
