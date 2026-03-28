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

    /// <summary>Notifies the dashboard that a new raffle was created.</summary>
    Task BroadcastRaffleCreatedAsync(Raffle raffle, CancellationToken ct = default);

    /// <summary>Notifies the dashboard of a new raffle entry.</summary>
    Task BroadcastRaffleEntryAsync(int raffleId, string username, int entryCount, CancellationToken ct = default);

    /// <summary>Notifies the dashboard that a raffle winner was drawn.</summary>
    Task BroadcastRaffleDrawnAsync(int raffleId, string winnerName, int totalEntries, CancellationToken ct = default);

    /// <summary>Notifies the dashboard that a raffle was cancelled.</summary>
    Task BroadcastRaffleCancelledAsync(int raffleId, CancellationToken ct = default);

    /// <summary>Notifies dashboard that a winner was drawn but is pending verification.</summary>
    Task BroadcastRaffleDrawPendingAsync(int raffleId, string winnerName, string winnerTwitchId, int totalEntries, int drawNumber, CancellationToken ct = default);

    /// <summary>Notifies dashboard that a pending winner was accepted (raffle stays open).</summary>
    Task BroadcastRaffleWinnerAcceptedAsync(int raffleId, string winnerName, int drawNumber, CancellationToken ct = default);

    /// <summary>Notifies dashboard that the raffle has been ended (final close).</summary>
    Task BroadcastRaffleEndedAsync(int raffleId, CancellationToken ct = default);

    /// <summary>Notifies dashboard that a counter value changed.</summary>
    Task BroadcastCounterUpdatedAsync(int counterId, string name, int value, CancellationToken ct = default);

    /// <summary>Notifies dashboard that a raid occurred.</summary>
    Task BroadcastRaidEventAsync(string username, int viewers, CancellationToken ct = default);

    /// <summary>Notifies dashboard of a gift sub event.</summary>
    Task BroadcastGiftSubEventAsync(string gifter, int count, int tier, CancellationToken ct = default);

    /// <summary>Notifies dashboard of a resub event.</summary>
    Task BroadcastResubEventAsync(string username, int months, int tier, string? message, CancellationToken ct = default);
}
