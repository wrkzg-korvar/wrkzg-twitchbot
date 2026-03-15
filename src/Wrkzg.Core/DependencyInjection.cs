using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Services;

namespace Wrkzg.Core;

/// <summary>
/// Registers all Core services in the DI container.
/// Called from Wrkzg.Host/Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Command Processor (Singleton — maintains cooldown state in-memory)
        services.AddSingleton<ICommandProcessor, CommandProcessor>();

        // Chat Message Pipeline (Singleton — orchestrates message processing)
        services.AddSingleton<ChatMessagePipeline>();

        // TODO: Future services (registered in later steps):
        // services.AddSingleton<IChatGameManager, ChatGameManager>();
        // services.AddSingleton<IUserTrackingService, UserTrackingService>();
        // services.AddSingleton<IRaffleService, RaffleService>();
        // services.AddSingleton<IPollService, PollService>();
        // services.AddSingleton<IPointsService, PointsService>();

        return services;
    }
}
