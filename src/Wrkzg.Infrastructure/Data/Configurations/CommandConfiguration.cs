using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class CommandConfiguration : IEntityTypeConfiguration<Command>
{
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
                     ?? System.Array.Empty<string>())
            .HasColumnType("TEXT");

        builder.Property(c => c.PermissionLevel)
            .HasConversion<int>();
    }
}
