using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Background service that awards points and tracks watch time for active viewers.
/// Also exposes MarkUserActive() for the ChatMessagePipeline to call.
/// </summary>
public interface IUserTrackingService : IHostedService
{
    /// <summary>
    /// Marks a user as "active" so they receive points on the next tracking tick.
    /// Called by ChatMessagePipeline when a chat message is processed.
    /// </summary>
    void MarkUserActive(string twitchId);

    /// <summary>
    /// Returns the Twitch user IDs of all users currently tracked as active
    /// (chatters from Helix + message senders within the last 5 minutes).
    /// Used by the Dashboard live viewer list.
    /// </summary>
    /// <returns>A read-only list of active Twitch user IDs.</returns>
    IReadOnlyList<string> GetActiveUserIds();
}
