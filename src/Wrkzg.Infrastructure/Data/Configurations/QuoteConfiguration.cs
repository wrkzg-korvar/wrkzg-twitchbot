using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Quote"/> model.
/// </summary>
public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    /// <summary>Configures the schema for the Quotes table.</summary>
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Number).IsRequired();
        builder.HasIndex(q => q.Number).IsUnique();
        builder.Property(q => q.Text).IsRequired().HasMaxLength(500);
        builder.Property(q => q.QuotedUser).IsRequired().HasMaxLength(100);
        builder.Property(q => q.SavedBy).IsRequired().HasMaxLength(100);
        builder.Property(q => q.GameName).HasMaxLength(200);
    }
}
