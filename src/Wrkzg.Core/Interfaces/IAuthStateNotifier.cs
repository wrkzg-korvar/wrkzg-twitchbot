using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Broadcasts authentication state changes to the frontend via SignalR.
/// Implemented in Wrkzg.Api as SignalRAuthNotifier.
/// </summary>
public interface IAuthStateNotifier
{
    /// <summary>
    /// Sends the updated authentication state to connected dashboard clients.
    /// </summary>
    /// <param name="state">The current authentication state to broadcast.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the notification has been sent.</returns>
    Task NotifyAuthStateChangedAsync(AuthState state, CancellationToken ct = default);
}
