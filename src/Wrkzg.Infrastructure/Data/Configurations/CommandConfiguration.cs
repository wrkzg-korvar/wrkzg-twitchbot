using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Command"/> model.
/// Configures JSON value conversion for the Aliases array stored as TEXT in SQLite.
/// </summary>
public class CommandConfiguration : IEntityTypeConfiguration<Command>
{
    /// <summary>Configures the schema for the Commands table.</summary>
    public void Configure(EntityTypeBuilder<Command> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Trigger).IsUnique();

        builder.Property(c => c.Trigger).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ResponseTemplate).IsRequired().HasMaxLength(500);

        // Store string[] Aliases as JSON text in SQLite
        builder.Property(c => c.Aliases)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
                     ?? Array.Empty<string>())
            .HasColumnType("TEXT")
            .Metadata.SetValueComparer(StringArrayComparer());

        builder.Property(c => c.PermissionLevel)
            .HasConversion<int>();
    }

    /// <summary>Creates a value comparer for string arrays used in JSON-to-TEXT value conversions.</summary>
    internal static ValueComparer<string[]> StringArrayComparer() => new(
        (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
        v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        v => v.ToArray());
}
