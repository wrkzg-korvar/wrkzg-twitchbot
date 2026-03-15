using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Api.Services;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IAuthStateNotifier, SignalRAuthNotifier>();
        services.AddSingleton<IChatEventBroadcaster, SignalRChatBroadcaster>();

        return services;
    }
}