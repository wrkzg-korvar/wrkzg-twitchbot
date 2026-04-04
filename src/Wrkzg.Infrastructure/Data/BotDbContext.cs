using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Wrkzg bot database.
/// Uses SQLite, stored in the OS application data directory.
/// </summary>
public class BotDbContext : DbContext
{
    /// <summary>Gets the set of Twitch users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the set of custom chat commands.</summary>
    public DbSet<Command> Commands => Set<Command>();

    /// <summary>Gets the set of raffles.</summary>
    public DbSet<Raffle> Raffles => Set<Raffle>();

    /// <summary>Gets the set of raffle entries.</summary>
    public DbSet<RaffleEntry> RaffleEntries => Set<RaffleEntry>();

    /// <summary>Gets the set of raffle draw results.</summary>
    public DbSet<RaffleDraw> RaffleDraws => Set<RaffleDraw>();

    /// <summary>Gets the set of polls.</summary>
    public DbSet<Poll> Polls => Set<Poll>();

    /// <summary>Gets the set of poll votes.</summary>
    public DbSet<PollVote> PollVotes => Set<PollVote>();

    /// <summary>Gets the set of key-value settings.</summary>
    public DbSet<Setting> Settings => Set<Setting>();

    /// <summary>Gets the set of system command overrides.</summary>
    public DbSet<SystemCommandOverride> SystemCommandOverrides => Set<SystemCommandOverride>();

    /// <summary>Gets the set of timed (recurring) chat messages.</summary>
    public DbSet<TimedMessage> TimedMessages => Set<TimedMessage>();

    /// <summary>Gets the set of stream counters.</summary>
    public DbSet<Counter> Counters => Set<Counter>();

    /// <summary>Gets the set of saved quotes.</summary>
    public DbSet<Quote> Quotes => Set<Quote>();

    /// <summary>Gets the set of channel point reward configurations.</summary>
    public DbSet<ChannelPointReward> ChannelPointRewards => Set<ChannelPointReward>();

    /// <summary>Gets the set of user roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Gets the set of user-to-role assignments.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Gets the set of trivia questions.</summary>
    public DbSet<TriviaQuestion> TriviaQuestions => Set<TriviaQuestion>();

    /// <summary>Gets the set of stream sessions.</summary>
    public DbSet<StreamSession> StreamSessions => Set<StreamSession>();

    /// <summary>Gets the set of category segments within stream sessions.</summary>
    public DbSet<CategorySegment> CategorySegments => Set<CategorySegment>();

    /// <summary>Gets the set of viewer count snapshots.</summary>
    public DbSet<ViewerSnapshot> ViewerSnapshots => Set<ViewerSnapshot>();

    /// <summary>Gets the set of song requests.</summary>
    public DbSet<SongRequest> SongRequests => Set<SongRequest>();

    /// <summary>Gets the set of hotkey bindings.</summary>
    public DbSet<HotkeyBinding> HotkeyBindings => Set<HotkeyBinding>();

    /// <summary>Gets the set of effect lists.</summary>
    public DbSet<EffectList> EffectLists => Set<EffectList>();

    /// <summary>Gets the set of custom overlays.</summary>
    public DbSet<CustomOverlay> CustomOverlays => Set<CustomOverlay>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BotDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options configured with the SQLite connection string.</param>
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures warning suppression for EF Core diagnostics specific to the Wrkzg data model.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Suppress warning for RoleAutoAssignCriteria owned type with all-nullable columns.
        // This is by design — AutoAssign is optional and null when no criteria are set.
        options.ConfigureWarnings(w => w
            .Ignore(RelationalEventId.OptionalDependentWithAllNullPropertiesWarning)
            .Ignore(new EventId(20606, "OptionalDependentWithAllNullProperties")));
    }

    /// <summary>
    /// Applies all entity type configurations from the Infrastructure assembly.
    /// </summary>
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
