using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class RaffleConfiguration : IEntityTypeConfiguration<Raffle>
{
    public void Configure(EntityTypeBuilder<Raffle> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);

        builder.HasOne(r => r.Winner)
            .WithMany()
            .HasForeignKey(r => r.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Entries)
            .WithOne(e => e.Raffle)
            .HasForeignKey(e => e.RaffleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RaffleEntryConfiguration : IEntityTypeConfiguration<RaffleEntry>
{
    public void Configure(EntityTypeBuilder<RaffleEntry> builder)
    {
        builder.HasKey(e => e.Id);

        // Unique constraint: one entry per user per raffle
        builder.HasIndex(e => new { e.RaffleId, e.UserId }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(u => u.RaffleEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
