using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Wrkzg.Api.Hubs;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Api.Services;

/// <summary>
/// Broadcasts chat events and bot status to dashboard and overlay clients via SignalR.
/// </summary>
public class SignalRChatBroadcaster : IChatEventBroadcaster
{
    private readonly IHubContext<ChatHub> _hub;
    private readonly ChatMessageBuffer _buffer;

    /// <summary>Initializes the broadcaster with the SignalR hub context and message buffer.</summary>
    public SignalRChatBroadcaster(IHubContext<ChatHub> hub, ChatMessageBuffer buffer)
    {
        _hub = hub;
        _buffer = buffer;
    }

    /// <summary>
    /// Sends a SignalR event to both the "dashboard" and "overlay" groups.
    /// </summary>
    private Task BroadcastToAllAsync(string method, object payload, CancellationToken ct)
    {
        return Task.WhenAll(
            _hub.Clients.Group("dashboard").SendAsync(method, payload, ct),
            _hub.Clients.Group("overlay").SendAsync(method, payload, ct)
        );
    }

    /// <summary>Broadcasts a chat message to all connected dashboard and overlay clients.</summary>
    public Task BroadcastChatMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        _buffer.Add(message);

        return BroadcastToAllAsync("ChatMessage", new
        {
            userId = message.UserId,
            username = message.Username,
            displayName = message.DisplayName,
            content = message.Content,
            isMod = message.IsModerator,
            isSubscriber = message.IsSubscriber,
            isBroadcaster = message.IsBroadcaster,
            timestamp = message.Timestamp,
            emotes = message.Emotes
        }, ct);
    }

    /// <summary>Broadcasts the current viewer count to all connected clients.</summary>
    public Task BroadcastViewerCountAsync(int count, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("ViewerCount", count, ct);
    }

    /// <summary>Broadcasts a new follower event to all connected clients.</summary>
    public Task BroadcastFollowEventAsync(string username, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("FollowEvent", new
        {
            username,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts a new subscription event to all connected clients.</summary>
    public Task BroadcastSubscribeEventAsync(string username, int tier, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("SubscribeEvent", new
        {
            username,
            tier,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts the bot connection status to all connected clients.</summary>
    public Task BroadcastBotStatusAsync(object status, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("BotStatus", status, ct);
    }

    /// <summary>Broadcasts a poll creation event to all connected clients.</summary>
    public Task BroadcastPollCreatedAsync(Poll poll, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("PollCreated", new
        {
            id = poll.Id,
            question = poll.Question,
            options = poll.Options,
            durationSeconds = poll.DurationSeconds,
            endsAt = poll.EndsAt,
            createdBy = poll.CreatedBy,
            source = poll.Source.ToString()
        }, ct);
    }

    /// <summary>Broadcasts a poll vote event to all connected clients.</summary>
    public Task BroadcastPollVoteAsync(int pollId, int optionIndex, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("PollVote", new
        {
            pollId,
            optionIndex
        }, ct);
    }

    /// <summary>Broadcasts a poll ended event with results to all connected clients.</summary>
    public Task BroadcastPollEndedAsync(object results, CancellationToken ct = default)
    {
        // Wrap in an object with pollId for overlay identification.
        // The full results object is also included for dashboard consumption.
        return BroadcastToAllAsync("PollEnded", new { pollId = GetPollId(results), results }, ct);
    }

    private static int GetPollId(object results)
    {
        // PollResultsDto is a record with an Id property
        System.Reflection.PropertyInfo? idProp = results.GetType().GetProperty("Id");
        return idProp is not null ? (int)(idProp.GetValue(results) ?? 0) : 0;
    }

    /// <summary>Broadcasts a raffle creation event to all connected clients.</summary>
    public Task BroadcastRaffleCreatedAsync(Raffle raffle, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleCreated", new
        {
            id = raffle.Id,
            title = raffle.Title,
            keyword = raffle.Keyword,
            durationSeconds = raffle.DurationSeconds,
            entriesCloseAt = raffle.EntriesCloseAt,
            maxEntries = raffle.MaxEntries,
            createdBy = raffle.CreatedBy,
            entryCount = 0
        }, ct);
    }

    /// <summary>Broadcasts a raffle entry event to all connected clients.</summary>
    public Task BroadcastRaffleEntryAsync(int raffleId, string username, int entryCount, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleEntry", new
        {
            raffleId,
            username,
            entryCount
        }, ct);
    }

    /// <summary>Broadcasts a raffle drawn event with the winner name to all connected clients.</summary>
    public Task BroadcastRaffleDrawnAsync(int raffleId, string winnerName, int totalEntries, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleDrawn", new
        {
            raffleId,
            winnerName,
            totalEntries
        }, ct);
    }

    /// <summary>Broadcasts a raffle cancellation event to all connected clients.</summary>
    public Task BroadcastRaffleCancelledAsync(int raffleId, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleCancelled", new { raffleId }, ct);
    }

    /// <summary>Broadcasts a pending raffle draw for winner verification to all connected clients.</summary>
    public Task BroadcastRaffleDrawPendingAsync(int raffleId, string winnerName, string winnerTwitchId, int totalEntries, int drawNumber, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleDrawPending", new
        {
            raffleId,
            winnerName,
            twitchId = winnerTwitchId,
            totalEntries,
            drawNumber
        }, ct);
    }

    /// <summary>Broadcasts a raffle winner accepted event to all connected clients.</summary>
    public Task BroadcastRaffleWinnerAcceptedAsync(int raffleId, string winnerName, int drawNumber, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleWinnerAccepted", new
        {
            raffleId,
            winnerName,
            drawNumber
        }, ct);
    }

    /// <summary>Broadcasts a raffle ended event to all connected clients.</summary>
    public Task BroadcastRaffleEndedAsync(int raffleId, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleEnded", new { raffleId }, ct);
    }

    /// <summary>Broadcasts a counter value update to all connected clients.</summary>
    public Task BroadcastCounterUpdatedAsync(int counterId, string name, int value, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("CounterUpdated", new { counterId, name, value }, ct);
    }

    /// <summary>Broadcasts a raid event to all connected clients.</summary>
    public Task BroadcastRaidEventAsync(string username, int viewers, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaidEvent", new
        {
            username,
            viewers,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts a gift subscription event to all connected clients.</summary>
    public Task BroadcastGiftSubEventAsync(string gifter, int count, int tier, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("GiftSubEvent", new
        {
            username = gifter,
            count,
            tier,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts a resubscription event to all connected clients.</summary>
    public Task BroadcastResubEventAsync(string username, int months, int tier, string? message, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("ResubEvent", new
        {
            username,
            months,
            tier,
            message,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts a channel point redemption event to all connected clients.</summary>
    public Task BroadcastChannelPointRedemptionAsync(string username, string rewardTitle, int cost, string? userInput, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("ChannelPointRedemption", new
        {
            username,
            rewardTitle,
            cost,
            userInput,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts a song queue updated event to all connected clients.</summary>
    public Task BroadcastSongQueueUpdatedAsync(CancellationToken ct = default)
    {
        return BroadcastToAllAsync("SongQueueUpdated", new
        {
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>Broadcasts a stream online event to all connected clients.</summary>
    public Task BroadcastStreamOnlineAsync(string broadcasterName, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("StreamOnline", new
        {
            broadcaster = broadcasterName,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }
}
