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

    public Task BroadcastViewerCountAsync(int count, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("ViewerCount", count, ct);
    }

    public Task BroadcastFollowEventAsync(string username, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("FollowEvent", new
        {
            username,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    public Task BroadcastSubscribeEventAsync(string username, int tier, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("SubscribeEvent", new
        {
            username,
            tier,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    public Task BroadcastBotStatusAsync(object status, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("BotStatus", status, ct);
    }

    public Task BroadcastPollCreatedAsync(Poll poll, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("PollCreated", new
        {
            poll.Id,
            poll.Question,
            poll.Options,
            poll.DurationSeconds,
            poll.EndsAt,
            poll.CreatedBy,
            source = poll.Source.ToString()
        }, ct);
    }

    public Task BroadcastPollVoteAsync(int pollId, int optionIndex, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("PollVote", new
        {
            pollId,
            optionIndex
        }, ct);
    }

    public Task BroadcastPollEndedAsync(object results, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("PollEnded", results, ct);
    }

    public Task BroadcastRaffleCreatedAsync(Raffle raffle, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleCreated", new
        {
            raffle.Id,
            raffle.Title,
            raffle.Keyword,
            raffle.DurationSeconds,
            raffle.EntriesCloseAt,
            raffle.MaxEntries,
            raffle.CreatedBy,
            entryCount = 0
        }, ct);
    }

    public Task BroadcastRaffleEntryAsync(int raffleId, string username, int entryCount, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleEntry", new
        {
            raffleId,
            username,
            entryCount
        }, ct);
    }

    public Task BroadcastRaffleDrawnAsync(int raffleId, string winnerName, int totalEntries, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleDrawn", new
        {
            raffleId,
            winnerName,
            totalEntries
        }, ct);
    }

    public Task BroadcastRaffleCancelledAsync(int raffleId, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleCancelled", new { raffleId }, ct);
    }

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

    public Task BroadcastRaffleWinnerAcceptedAsync(int raffleId, string winnerName, int drawNumber, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleWinnerAccepted", new
        {
            raffleId,
            winnerName,
            drawNumber
        }, ct);
    }

    public Task BroadcastRaffleEndedAsync(int raffleId, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaffleEnded", new { raffleId }, ct);
    }

    public Task BroadcastCounterUpdatedAsync(int counterId, string name, int value, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("CounterUpdated", new { counterId, name, value }, ct);
    }

    public Task BroadcastRaidEventAsync(string username, int viewers, CancellationToken ct = default)
    {
        return BroadcastToAllAsync("RaidEvent", new
        {
            username,
            viewers,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

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
}
