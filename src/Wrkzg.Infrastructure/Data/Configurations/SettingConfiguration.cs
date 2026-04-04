using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Setting"/> model.
/// Seeds default application settings on initial database creation.
/// </summary>
public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    /// <summary>Configures the schema for the Settings table and seeds default values.</summary>
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.HasKey(s => s.Key);
        builder.Property(s => s.Key).HasMaxLength(100);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(500);

        // Seed default settings
        builder.HasData(
            new Setting { Key = "Bot.Channel", Value = "" },
            new Setting { Key = "Bot.BotUsername", Value = "" },
            new Setting { Key = "Bot.Port", Value = "5050" },
            new Setting { Key = "Points.PerMinute", Value = "10" },
            new Setting { Key = "Points.SubMultiplier", Value = "1.5" },
            new Setting { Key = "Points.FollowBonus", Value = "100" },
            new Setting { Key = "Updater.CheckOnStartup", Value = "true" },
            new Setting { Key = "Updater.AutoInstall", Value = "false" }
        );
    }
}
