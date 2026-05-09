using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Provides cached stream live status, polled once per minute from the Helix API.
/// All services that need to know if the stream is live MUST use this instead of
/// calling IBroadcasterHelixClient.GetStreamAsync() directly.
/// </summary>
public interface IStreamStatusProvider : IHostedService
{
    /// <summary>Gets a value indicating whether the stream is currently live. Updated every 60 seconds.</summary>
    bool IsLive { get; }

    /// <summary>Gets the current viewer count. 0 when offline.</summary>
    int ViewerCount { get; }

    /// <summary>Gets the full stream info from the last poll. Null when offline.</summary>
    StreamInfo? CurrentStream { get; }

    /// <summary>Gets the channel login name being monitored. Null if not yet configured.</summary>
    string? ChannelLogin { get; }

    /// <summary>Forces an immediate refresh of the stream status cache.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes once the refresh has finished.</returns>
    Task RefreshAsync(CancellationToken ct = default);
}
