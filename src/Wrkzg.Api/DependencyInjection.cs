using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Api.Security;
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

        // API Token (per-session authentication for Photino WebView)
        services.AddSingleton<ApiTokenService>();

        // CORS (restrictive: only Photino + Vite dev server)
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:5000", "http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}