using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Wrkzg.Api.Hubs;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Services;

/// <summary>
/// Broadcasts authentication state changes to all connected dashboard clients via SignalR.
/// </summary>
public class SignalRAuthNotifier : IAuthStateNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRAuthNotifier(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyAuthStateChangedAsync(AuthState state, CancellationToken ct = default)
    {
        await _hub.Clients.Group("dashboard").SendAsync("AuthStateChanged", new
        {
            tokenType = state.TokenType.ToString().ToLowerInvariant(),
            isAuthenticated = state.IsAuthenticated,
            twitchUsername = state.TwitchUsername,
            twitchDisplayName = state.TwitchDisplayName,
            twitchUserId = state.TwitchUserId,
            scopes = state.Scopes
        }, ct);
    }
}
