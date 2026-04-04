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
    /// <summary>Assigns the connecting client to the appropriate SignalR group based on the source query parameter.</summary>
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

    /// <summary>Removes the disconnecting client from all SignalR groups.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "overlay");
        await base.OnDisconnectedAsync(exception);
    }
}
