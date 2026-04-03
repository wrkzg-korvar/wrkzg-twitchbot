using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class EffectListConfiguration : IEntityTypeConfiguration<EffectList>
{
    public void Configure(EntityTypeBuilder<EffectList> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.TriggerTypeId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.TriggerConfig).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.ConditionsConfig).IsRequired().HasMaxLength(5000);
        builder.Property(e => e.EffectsConfig).IsRequired().HasMaxLength(5000);
    }
}
