using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="RaffleDraw"/> model.
/// </summary>
public class RaffleDrawConfiguration : IEntityTypeConfiguration<RaffleDraw>
{
    /// <summary>Configures the schema for the RaffleDraws table.</summary>
    public void Configure(EntityTypeBuilder<RaffleDraw> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.RedrawReason).HasMaxLength(200);

        builder.HasOne(d => d.Raffle)
            .WithMany(r => r.Draws)
            .HasForeignKey(d => d.RaffleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
