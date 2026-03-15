using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Wrkzg.Api.Hubs;

/// <summary>
/// SignalR hub for real-time communication with the dashboard frontend.
/// Clients join the "dashboard" group on connection to receive auth state updates.
/// </summary>
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(System.Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
        await base.OnDisconnectedAsync(exception);
    }
}