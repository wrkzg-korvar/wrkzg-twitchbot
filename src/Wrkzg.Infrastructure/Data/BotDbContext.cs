using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Wrkzg bot database.
/// Uses SQLite, stored in the OS application data directory.
/// </summary>
public class BotDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Command> Commands => Set<Command>();
    public DbSet<Raffle> Raffles => Set<Raffle>();
    public DbSet<RaffleEntry> RaffleEntries => Set<RaffleEntry>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();
    public DbSet<Setting> Settings => Set<Setting>();

    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.ApplyConfigurationsFromAssembly(typeof(BotDbContext).Assembly);
    }

    /// <summary>
    /// Returns the platform-appropriate database file path.
    /// Windows: %APPDATA%\Wrkzg\bot.db
    /// macOS:   ~/Library/Application Support/Wrkzg/bot.db
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        string appDataDir = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        string wrkzgDir = Path.Combine(appDataDir, "Wrkzg");
        Directory.CreateDirectory(wrkzgDir);

        return Path.Combine(wrkzgDir, "bot.db");
    }
}
