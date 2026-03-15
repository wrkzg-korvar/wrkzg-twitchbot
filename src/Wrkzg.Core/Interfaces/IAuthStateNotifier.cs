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
    Task NotifyAuthStateChangedAsync(AuthState state, CancellationToken ct = default);
}
