using Microsoft.Extensions.DependencyInjection;
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
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // System Commands (Singleton — stateless, use IServiceScopeFactory internally)
        services.AddSingleton<ISystemCommand, CommandsListCommand>();
        services.AddSingleton<ISystemCommand, PointsCommand>();
        services.AddSingleton<ISystemCommand, WatchtimeCommand>();
        services.AddSingleton<ISystemCommand, FollowageCommand>();
        services.AddSingleton<ISystemCommand, EditCommandCommand>();

        // Command Processor (Singleton — maintains cooldown state in-memory)
        services.AddSingleton<ICommandProcessor, CommandProcessor>();

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
