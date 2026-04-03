using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using TwitchLib.EventSub.Websockets.Extensions;
using Wrkzg.Core.Interfaces;
using Wrkzg.Infrastructure.Data;
using Wrkzg.Infrastructure.Repositories;
using Wrkzg.Infrastructure.Security;
using Wrkzg.Infrastructure.Twitch;

namespace Wrkzg.Infrastructure;

public static class DependencyInjection
{
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

        // Twitch Auth Handler (DelegatingHandler for Helix API requests)
        services.AddTransient<TwitchAuthHandler>();

        // Twitch Helix API Client (with TwitchAuthHandler for auto Bearer token + Client-Id)
        services.AddHttpClient<ITwitchHelixClient, TwitchHelixClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.twitch.tv/helix/");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler<TwitchAuthHandler>()
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

        // Stream Analytics (IHostedService — polls viewer count + category every 60s)
        services.AddHostedService<Wrkzg.Infrastructure.Services.StreamAnalyticsService>();

        return services;
    }
}