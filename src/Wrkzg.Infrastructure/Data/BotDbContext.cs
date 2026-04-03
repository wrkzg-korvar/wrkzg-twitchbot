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
    public DbSet<RaffleDraw> RaffleDraws => Set<RaffleDraw>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollVote> PollVotes => Set<PollVote>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<SystemCommandOverride> SystemCommandOverrides => Set<SystemCommandOverride>();
    public DbSet<TimedMessage> TimedMessages => Set<TimedMessage>();
    public DbSet<Counter> Counters => Set<Counter>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<ChannelPointReward> ChannelPointRewards => Set<ChannelPointReward>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<TriviaQuestion> TriviaQuestions => Set<TriviaQuestion>();
    public DbSet<StreamSession> StreamSessions => Set<StreamSession>();
    public DbSet<CategorySegment> CategorySegments => Set<CategorySegment>();
    public DbSet<ViewerSnapshot> ViewerSnapshots => Set<ViewerSnapshot>();
    public DbSet<SongRequest> SongRequests => Set<SongRequest>();
    public DbSet<HotkeyBinding> HotkeyBindings => Set<HotkeyBinding>();

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
