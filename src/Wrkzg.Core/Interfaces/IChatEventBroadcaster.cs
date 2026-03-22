using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Broadcasts chat events and bot status to the dashboard via SignalR.
/// Implemented in Wrkzg.Api as SignalRChatBroadcaster.
/// Lives in Core so Infrastructure services can broadcast without depending on Api.
/// </summary>
public interface IChatEventBroadcaster
{
    Task BroadcastChatMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task BroadcastViewerCountAsync(int count, CancellationToken ct = default);
    Task BroadcastFollowEventAsync(string username, CancellationToken ct = default);
    Task BroadcastSubscribeEventAsync(string username, int tier, CancellationToken ct = default);
    Task BroadcastBotStatusAsync(object status, CancellationToken ct = default);

    /// <summary>Notifies the dashboard that a new poll was created.</summary>
    Task BroadcastPollCreatedAsync(Poll poll, CancellationToken ct = default);

    /// <summary>Notifies the dashboard of a live vote update.</summary>
    Task BroadcastPollVoteAsync(int pollId, int optionIndex, CancellationToken ct = default);

    /// <summary>Notifies the dashboard that a poll has ended with results.</summary>
    Task BroadcastPollEndedAsync(object results, CancellationToken ct = default);
}
