using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Color).HasMaxLength(7);
        builder.Property(r => r.Icon).HasMaxLength(10);
        builder.OwnsOne(r => r.AutoAssign, a =>
        {
            a.Property(c => c.MinWatchedMinutes).HasColumnName("AutoAssign_MinWatchedMinutes");
            a.Property(c => c.MinPoints).HasColumnName("AutoAssign_MinPoints");
            a.Property(c => c.MinMessages).HasColumnName("AutoAssign_MinMessages");
            a.Property(c => c.MustBeFollower).HasColumnName("AutoAssign_MustBeFollower");
            a.Property(c => c.MustBeSubscriber).HasColumnName("AutoAssign_MustBeSubscriber");
        });
    }
}
