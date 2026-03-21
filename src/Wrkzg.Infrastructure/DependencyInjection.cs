using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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

        // Secure Storage (platform-specific)
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISecureStorage, WindowsSecureStorage>();
        }
        else if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<ISecureStorage, MacOsSecureStorage>();
        }
        // else: No ISecureStorage on unsupported platforms.
        // Tests provide their own fake; the runtime app only runs on Windows/macOS.

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

        return services;
    }
}