using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Allows triggering a bot connection attempt from outside the hosted service.
/// Used by the Setup Wizard and Settings page to connect after configuration changes.
/// </summary>
public interface IBotConnectionService
{
    /// <summary>
    /// Attempts to connect the bot to IRC if credentials and channel are configured.
    /// Returns true if the connection was initiated, false if prerequisites are missing.
    /// </summary>
    Task<bool> TryConnectAsync(CancellationToken ct = default);
}
