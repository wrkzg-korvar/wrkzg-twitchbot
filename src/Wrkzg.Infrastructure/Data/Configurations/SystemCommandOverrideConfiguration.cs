using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class SystemCommandOverrideConfiguration : IEntityTypeConfiguration<SystemCommandOverride>
{
    public void Configure(EntityTypeBuilder<SystemCommandOverride> builder)
    {
        builder.HasKey(x => x.Trigger);
        builder.Property(x => x.Trigger).HasMaxLength(50);
        builder.Property(x => x.CustomResponseTemplate).HasMaxLength(500);
    }
}
