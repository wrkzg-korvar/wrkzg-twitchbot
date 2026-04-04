using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="SystemCommandOverride"/> model.
/// </summary>
public class SystemCommandOverrideConfiguration : IEntityTypeConfiguration<SystemCommandOverride>
{
    /// <summary>Configures the schema for the SystemCommandOverrides table.</summary>
    public void Configure(EntityTypeBuilder<SystemCommandOverride> builder)
    {
        builder.HasKey(x => x.Trigger);
        builder.Property(x => x.Trigger).HasMaxLength(50);
        builder.Property(x => x.CustomResponseTemplate).HasMaxLength(500);
    }
}
