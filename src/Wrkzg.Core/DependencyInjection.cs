using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.ChatGames;
using Wrkzg.Core.Effects;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Services;
using Wrkzg.Core.SystemCommands;

namespace Wrkzg.Core;

/// <summary>
/// Registers all Core services in the DI container.
/// Called from Wrkzg.Host/Program.cs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Core-layer services, system commands, chat games, and the effect engine in the DI container.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // System Commands (Singleton — stateless, use IServiceScopeFactory internally)
        services.AddSingleton<ISystemCommand, CommandsListCommand>();
        services.AddSingleton<ISystemCommand, PointsCommand>();
        services.AddSingleton<ISystemCommand, WatchtimeCommand>();
        services.AddSingleton<ISystemCommand, FollowageCommand>();
        services.AddSingleton<ISystemCommand, EditCommandCommand>();
        services.AddSingleton<ISystemCommand, PollCommand>();
        services.AddSingleton<ISystemCommand, VoteCommand>();
        services.AddSingleton<ISystemCommand, PollEndCommand>();
        services.AddSingleton<ISystemCommand, PollResultCommand>();
        services.AddSingleton<ISystemCommand, RaffleCommand>();
        services.AddSingleton<ISystemCommand, JoinRaffleCommand>();
        services.AddSingleton<ISystemCommand, DrawRaffleCommand>();
        services.AddSingleton<ISystemCommand, CancelRaffleCommand>();
        services.AddSingleton<ISystemCommand, UptimeCommand>();
        services.AddSingleton<ISystemCommand, ShoutoutCommand>();
        services.AddSingleton<ISystemCommand, QuoteCommand>();
        services.AddSingleton<ISystemCommand, SongRequestCommand>();
        services.AddSingleton<ISystemCommand, SkipSongCommand>();
        services.AddSingleton<ISystemCommand, QueueCommand>();
        services.AddSingleton<ISystemCommand, CurrentSongCommand>();

        // Poll System
        services.AddScoped<PollService>();
        services.AddHostedService<PollTimerService>();

        // Raffle System
        services.AddScoped<RaffleService>();
        services.AddHostedService<RaffleTimerService>();

        // Timed Messages (Singleton + IHostedService)
        services.AddSingleton<TimedMessageService>();
        services.AddHostedService(sp => sp.GetRequiredService<TimedMessageService>());

        // Spam Filter
        services.AddScoped<SpamFilterService>();

        // Role Evaluation (Singleton — uses IServiceScopeFactory internally)
        services.AddSingleton<RoleEvaluationService>();

        // Command Processor (Singleton — maintains cooldown state in-memory)
        services.AddSingleton<ICommandProcessor, CommandProcessor>();

        // Chat Games (Singleton — each game manages its own state)
        services.AddSingleton<IChatGame, HeistGame>();
        services.AddSingleton<IChatGame, DuelGame>();
        services.AddSingleton<IChatGame, SlotsGame>();
        services.AddSingleton<IChatGame, RouletteGame>();
        services.AddSingleton<IChatGame, TriviaGame>();

        // Effect System — Built-in Trigger Types
        services.AddSingleton<ITriggerType, Effects.Triggers.CommandTrigger>();
        services.AddSingleton<ITriggerType, Effects.Triggers.EventTrigger>();
        services.AddSingleton<ITriggerType, Effects.Triggers.KeywordTrigger>();
        services.AddSingleton<ITriggerType, Effects.Triggers.HotkeyTrigger>();
        services.AddSingleton<ITriggerType, Effects.Triggers.ChannelPointTrigger>();

        // Effect System — Built-in Condition Types
        services.AddSingleton<IConditionType, Effects.Conditions.RoleCheckCondition>();
        services.AddSingleton<IConditionType, Effects.Conditions.PointsCheckCondition>();
        services.AddSingleton<IConditionType, Effects.Conditions.RandomChanceCondition>();
        services.AddSingleton<IConditionType, Effects.Conditions.StreamStatusCondition>();

        // Effect System — Built-in Effect Types
        services.AddSingleton<IEffectType, Effects.EffectTypes.ChatMessageEffect>();
        services.AddSingleton<IEffectType, Effects.EffectTypes.WaitEffect>();
        services.AddSingleton<IEffectType, Effects.EffectTypes.CounterEffect>();
        services.AddSingleton<IEffectType, Effects.EffectTypes.AlertEffect>();
        services.AddSingleton<IEffectType, Effects.EffectTypes.VariableEffect>();

        // Effect System — Integration Effect Types (v2.1.0)
        services.AddSingleton<IEffectType, Effects.EffectTypes.DiscordSendMessageEffect>();
        services.AddSingleton<IEffectType, Effects.EffectTypes.DiscordSendEmbedEffect>();

        // Effect Engine (Singleton — evaluates Trigger → Condition → Effect chains)
        services.AddSingleton<EffectEngine>();

        // Song Request Service (Singleton — manages song queue)
        services.AddSingleton<SongRequestService>();

        // Chat Game Manager (Singleton — manages all chat games)
        services.AddSingleton<ChatGameManager>();

        // Chat Message Pipeline (Singleton — orchestrates message processing)
        services.AddSingleton<ChatMessagePipeline>();

        // Chat Message Buffer (Singleton — holds last N messages for dashboard reload)
        services.AddSingleton<ChatMessageBuffer>();

        // UserTrackingService (Singleton + IHostedService)
        // Triple registration: same instance as IUserTrackingService, IHostedService, and concrete type
        services.AddSingleton<UserTrackingService>();
        services.AddSingleton<IUserTrackingService>(sp => sp.GetRequiredService<UserTrackingService>());
        services.AddHostedService(sp => sp.GetRequiredService<UserTrackingService>());

        return services;
    }
}
