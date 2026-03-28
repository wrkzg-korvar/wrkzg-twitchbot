using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Wrkzg.Api.Hubs;

/// <summary>
/// SignalR hub for real-time communication with the dashboard and OBS overlays.
/// Clients join either the "dashboard" group (authenticated) or "overlay" group (no auth).
/// The source is determined by the ?source=overlay query parameter.
/// </summary>
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        string? source = Context.GetHttpContext()?.Request.Query["source"];

        if (string.Equals(source, "overlay", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "overlay");
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "overlay");
        await base.OnDisconnectedAsync(exception);
    }
}
