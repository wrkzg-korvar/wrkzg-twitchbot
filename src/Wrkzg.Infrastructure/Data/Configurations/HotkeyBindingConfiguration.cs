using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="HotkeyBinding"/> model.
/// </summary>
public class HotkeyBindingConfiguration : IEntityTypeConfiguration<HotkeyBinding>
{
    /// <summary>Configures the schema for the HotkeyBindings table.</summary>
    public void Configure(EntityTypeBuilder<HotkeyBinding> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.KeyCombination).IsRequired().HasMaxLength(100);
        builder.Property(h => h.ActionType).IsRequired().HasMaxLength(50);
        builder.Property(h => h.ActionPayload).HasMaxLength(500);
        builder.Property(h => h.Description).HasMaxLength(200);
    }
}
