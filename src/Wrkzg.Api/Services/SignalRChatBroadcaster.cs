using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Wrkzg.Api.Hubs;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Api.Services;

/// <summary>
/// Broadcasts chat events and bot status to dashboard clients via SignalR.
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

    public Task BroadcastChatMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        _buffer.Add(message);

        return _hub.Clients.Group("dashboard").SendAsync("ChatMessage", new
        {
            userId = message.UserId,
            username = message.Username,
            displayName = message.DisplayName,
            content = message.Content,
            isMod = message.IsModerator,
            isSubscriber = message.IsSubscriber,
            isBroadcaster = message.IsBroadcaster,
            timestamp = message.Timestamp
        }, ct);
    }

    public Task BroadcastViewerCountAsync(int count, CancellationToken ct = default)
    {
        return _hub.Clients.Group("dashboard").SendAsync("ViewerCount", count, ct);
    }

    public Task BroadcastFollowEventAsync(string username, CancellationToken ct = default)
    {
        return _hub.Clients.Group("dashboard").SendAsync("FollowEvent", new { username }, ct);
    }

    public Task BroadcastSubscribeEventAsync(string username, int tier, CancellationToken ct = default)
    {
        return _hub.Clients.Group("dashboard").SendAsync("SubscribeEvent", new { username, tier }, ct);
    }

    public Task BroadcastBotStatusAsync(object status, CancellationToken ct = default)
    {
        return _hub.Clients.Group("dashboard").SendAsync("BotStatus", status, ct);
    }

    public Task BroadcastPollCreatedAsync(Poll poll, CancellationToken ct = default)
    {
        return _hub.Clients.Group("dashboard").SendAsync("PollCreated", new
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
        return _hub.Clients.Group("dashboard").SendAsync("PollVote", new
        {
            pollId,
            optionIndex
        }, ct);
    }

    public Task BroadcastPollEndedAsync(object results, CancellationToken ct = default)
    {
        return _hub.Clients.Group("dashboard").SendAsync("PollEnded", results, ct);
    }
}
