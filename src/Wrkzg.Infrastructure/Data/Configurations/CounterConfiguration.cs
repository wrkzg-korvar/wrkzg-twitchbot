using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Counter"/> model.
/// </summary>
public class CounterConfiguration : IEntityTypeConfiguration<Counter>
{
    /// <summary>Configures the schema for the Counters table.</summary>
    public void Configure(EntityTypeBuilder<Counter> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Trigger).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Trigger).IsUnique();
        builder.Property(c => c.ResponseTemplate).HasMaxLength(200).HasDefaultValue("{name}: {value}");
    }
}
