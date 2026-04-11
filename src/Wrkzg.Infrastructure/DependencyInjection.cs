using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TwitchLib.EventSub.Websockets.Extensions;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;
using Wrkzg.Infrastructure.Import;
using Wrkzg.Infrastructure.Repositories;
using Wrkzg.Infrastructure.Security;
using Wrkzg.Infrastructure.Integrations;
using Wrkzg.Infrastructure.Services;
using Wrkzg.Infrastructure.Twitch;

namespace Wrkzg.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services including the SQLite database, repositories,
    /// platform-specific secure storage, Twitch clients, and hosted background services.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="config">The application configuration for reading connection and API settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // SQLite Database
        string dbPath = BotDbContext.GetDefaultDatabasePath();
        services.AddDbContext<BotDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Repositories (Scoped — one per request/operation)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICommandRepository, CommandRepository>();
        services.AddScoped<IRaffleRepository, RaffleRepository>();
        services.AddScoped<IPollRepository, PollRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISystemCommandOverrideRepository, SystemCommandOverrideRepository>();
        services.AddScoped<ITimedMessageRepository, TimedMessageRepository>();
        services.AddScoped<ICounterRepository, CounterRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IChannelPointRewardRepository, ChannelPointRewardRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITriviaQuestionRepository, TriviaQuestionRepository>();
        services.AddScoped<IStreamAnalyticsRepository, StreamAnalyticsRepository>();
        services.AddScoped<ISongRequestRepository, SongRequestRepository>();
        services.AddScoped<IHotkeyBindingRepository, HotkeyBindingRepository>();
        services.AddScoped<IEffectListRepository, EffectListRepository>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddScoped<ICustomOverlayRepository, CustomOverlayRepository>();

        // Background import job service (Singleton — manages in-memory job state)
        services.AddSingleton<IImportJobService, ImportJobService>();

        // Secure Storage + Hotkey Listener (platform-specific)
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISecureStorage, WindowsSecureStorage>();
            services.AddSingleton<IHotkeyListener, Wrkzg.Infrastructure.Hotkeys.WindowsHotkeyListener>();
        }
        else if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<ISecureStorage, MacOsSecureStorage>();
            services.AddSingleton<IHotkeyListener, Wrkzg.Infrastructure.Hotkeys.MacOsHotkeyListener>();
        }
        else
        {
            // Explicit fallback: throws PlatformNotSupportedException at usage time
            // instead of silently failing with a DI resolution crash.
            // Tests provide their own fake via DI override.
            services.AddSingleton<ISecureStorage, UnsupportedPlatformSecureStorage>();
            services.AddSingleton<IHotkeyListener, Wrkzg.Infrastructure.Hotkeys.NoOpHotkeyListener>();
        }

        // Hotkey Services
        services.AddSingleton<Wrkzg.Core.Services.HotkeyActionExecutor>();
        services.AddSingleton<Wrkzg.Infrastructure.Hotkeys.HotkeyListenerService>();
        services.AddHostedService(sp => sp.GetRequiredService<Wrkzg.Infrastructure.Hotkeys.HotkeyListenerService>());

        // Twitch OAuth Service (own HttpClient with resilience pipeline)
        services.AddHttpClient<ITwitchOAuthService, TwitchOAuthService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddStandardResilienceHandler();

        // Broadcaster Helix API Client (Broadcaster token — stream info, polls, channel points)
        services.AddHttpClient<IBroadcasterHelixClient, BroadcasterHelixClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.twitch.tv/helix/");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler(sp => new TwitchAuthHandler(
            TokenType.Broadcaster,
            sp.GetRequiredService<ISecureStorage>(),
            sp.GetRequiredService<ITwitchOAuthService>(),
            sp.GetRequiredService<IAuthStateNotifier>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TwitchAuthHandler>>()))
        .AddStandardResilienceHandler();

        // Bot Helix API Client (Bot token — announcements, timeouts)
        services.AddHttpClient<IBotHelixClient, BotHelixClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.twitch.tv/helix/");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler(sp => new TwitchAuthHandler(
            TokenType.Bot,
            sp.GetRequiredService<ISecureStorage>(),
            sp.GetRequiredService<ITwitchOAuthService>(),
            sp.GetRequiredService<IAuthStateNotifier>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TwitchAuthHandler>>()))
        .AddStandardResilienceHandler();

        // Twitch Chat Client (Singleton — one IRC connection per app)
        services.AddSingleton<ITwitchChatClient, TwitchChatClient>();

        // Bot Connection Service (IHostedService — manages IRC lifecycle)
        services.AddSingleton<BotConnectionService>();
        services.AddHostedService<BotConnectionService>(sp => sp.GetRequiredService<BotConnectionService>());
        services.AddSingleton<IBotConnectionService>(sp => sp.GetRequiredService<BotConnectionService>());

        // EventSub WebSocket (TwitchLib — manages WebSocket, keepalive, reconnect)
        services.AddTwitchLibEventSubWebsockets();
        services.AddHostedService<EventSubConnectionService>();

        // Emote Service (IHostedService — caches global + channel emotes, refreshes every 30m)
        services.AddSingleton<EmoteService>();
        services.AddSingleton<IEmoteService>(sp => sp.GetRequiredService<EmoteService>());
        services.AddHostedService(sp => sp.GetRequiredService<EmoteService>());

        // Stream Analytics (IHostedService — polls viewer count + category every 60s)
        services.AddHostedService<Wrkzg.Infrastructure.Services.StreamAnalyticsService>();

        // OBS WebSocket Integration (Singleton — one connection per app)
        services.AddSingleton<ObsWebSocketService>();
        services.AddSingleton<IObsWebSocketService>(sp => sp.GetRequiredService<ObsWebSocketService>());

        return services;
    }
}