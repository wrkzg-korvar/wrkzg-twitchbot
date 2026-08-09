using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848, CA1873 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Background service that checks every 30 seconds if a timed message should fire.
/// </summary>
public class TimedMessageService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStreamStatusProvider _streamStatus;
    private readonly ITwitchChatClient _chat;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TimedMessageService> _logger;

    // Monotonic count of chat lines since the service started (never reset). Combined with a
    // per-timer baseline (_chatLinesAtLastFire) to gate on "chat lines since this timer last
    // fired" — not merely "chat lines within the last poll window".
    private long _totalChatLines;
    private readonly Dictionary<int, long> _chatLinesAtLastFire = new();

    // Live-session tracking. _liveSince is the start of the current live session (the
    // offline→online edge, or the first tick when the bot starts up already live). It is used
    // as the schedule anchor so that overdue timers do not all fire at once when the stream
    // goes live.
    private bool _wasLive;
    private DateTimeOffset? _liveSince;

    private string? _cachedBroadcasterId;
    private readonly SemaphoreSlim _broadcasterIdLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of <see cref="TimedMessageService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="streamStatus">Cached stream live status provider — replaces direct Helix polling.</param>
    /// <param name="chat">The Twitch IRC chat client for sending timed messages.</param>
    /// <param name="timeProvider">Clock abstraction — injected so scheduling is deterministic and testable.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public TimedMessageService(
        IServiceScopeFactory scopeFactory,
        IStreamStatusProvider streamStatus,
        ITwitchChatClient chat,
        TimeProvider timeProvider,
        ILogger<TimedMessageService> logger)
    {
        _scopeFactory = scopeFactory;
        _streamStatus = streamStatus;
        _chat = chat;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Called by ChatMessagePipeline for every chat message to track activity.</summary>
    public void IncrementChatLineCounter()
    {
        Interlocked.Increment(ref _totalChatLines);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimedMessageService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_chat.IsConnected)
                {
                    await CheckAndFireTimersAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in TimedMessageService");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    /// <summary>
    /// Evaluates all enabled timers once and fires those that are due. Internal for unit testing;
    /// invoked on a fixed cadence by <see cref="ExecuteAsync"/> in production.
    /// </summary>
    internal async Task CheckAndFireTimersAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ITimedMessageRepository repo = scope.ServiceProvider.GetRequiredService<ITimedMessageRepository>();
        IBotHelixClient botHelix = scope.ServiceProvider.GetRequiredService<IBotHelixClient>();

        bool isLive = _streamStatus.IsLive;
        DateTimeOffset now = _timeProvider.GetUtcNow();

        // Track the start of the current live session: the offline→online edge, and also the
        // first tick when the bot starts up while already live (_wasLive defaults to false).
        // Anchoring timers to this moment prevents the whole set from becoming overdue — and
        // firing together — the instant the stream goes live.
        if (isLive && !_wasLive)
        {
            _liveSince = now;
        }
        _wasLive = isLive;

        string? broadcasterId = null;

        // Resolve broadcaster ID from token (cached, thread-safe) — only needed for announcements
        try
        {
            broadcasterId = await ResolveBroadcasterIdAsync(scope.ServiceProvider, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to resolve broadcaster ID for announcements");
        }

        IReadOnlyList<TimedMessage> timers = await repo.GetEnabledAsync(ct);
        long totalChatLines = Interlocked.Read(ref _totalChatLines);

        foreach (TimedMessage timer in timers)
        {
            if (isLive && !timer.RunWhenOnline)
            {
                continue;
            }
            if (!isLive && !timer.RunWhenOffline)
            {
                continue;
            }

            // Schedule anchor. When live, never let an offline gap — or a LastFiredAt left over
            // from a previous stream — count as backlog: otherwise every timer becomes overdue
            // at the same moment the stream goes live and they all fire together. Anchoring to
            // the start of the live session means each timer's first fire is one interval after
            // going live, then spaced by its own interval.
            DateTimeOffset? anchor = timer.LastFiredAt;
            if (isLive && _liveSince.HasValue && (anchor is null || _liveSince.Value > anchor.Value))
            {
                anchor = _liveSince.Value;
            }

            if (anchor.HasValue && (now - anchor.Value).TotalMinutes < timer.IntervalMinutes)
            {
                continue;
            }

            // Require a minimum amount of chat activity SINCE this timer last fired (not merely
            // within the last poll window), so a quiet chat is not spammed and the bot does not
            // repeat itself without intervening conversation.
            if (timer.MinChatLines > 0)
            {
                long baselineChatLines = _chatLinesAtLastFire.TryGetValue(timer.Id, out long lastFireChatLines) ? lastFireChatLines : 0;
                if (totalChatLines - baselineChatLines < timer.MinChatLines)
                {
                    continue;
                }
            }

            if (timer.Messages.Length == 0)
            {
                continue;
            }

            string message = timer.Messages[timer.NextMessageIndex % timer.Messages.Length];
            if (timer.IsAnnouncement)
            {
                if (broadcasterId is null)
                {
                    _logger.LogWarning(
                        "Cannot send announcement for timer '{Name}': broadcasterId not resolved — falling back to normal message",
                        timer.Name);
                    await _chat.SendMessageAsync(message, ct);
                }
                else
                {
                    _logger.LogInformation("Attempting announcement for timer '{Name}'", timer.Name);

                    string color = string.IsNullOrWhiteSpace(timer.AnnouncementColor) ? "primary" : timer.AnnouncementColor;
                    bool success = await botHelix.SendAnnouncementAsync(broadcasterId, message, color, ct);
                    if (success)
                    {
                        _logger.LogInformation("Announcement sent successfully for timer '{Name}'", timer.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Announcement failed for timer '{Name}', falling back to normal message", timer.Name);
                        await _chat.SendMessageAsync(message, ct);
                    }
                }
            }
            else
            {
                await _chat.SendMessageAsync(message, ct);
            }

            _chatLinesAtLastFire[timer.Id] = totalChatLines;
            timer.NextMessageIndex = (timer.NextMessageIndex + 1) % timer.Messages.Length;
            timer.LastFiredAt = now;
            await repo.UpdateAsync(timer, ct);

            _logger.LogInformation("Fired timed message '{Name}': {Message}",
                timer.Name, message.Length > 60 ? message[..60] + "…" : message);
        }
    }

    /// <summary>
    /// Resolves the broadcaster's Twitch user ID from the Broadcaster OAuth token.
    /// Thread-safe with caching.
    /// </summary>
    private async Task<string?> ResolveBroadcasterIdAsync(IServiceProvider services, CancellationToken ct)
    {
        if (_cachedBroadcasterId is not null)
        {
            return _cachedBroadcasterId;
        }

        await _broadcasterIdLock.WaitAsync(ct);
        try
        {
            if (_cachedBroadcasterId is not null)
            {
                return _cachedBroadcasterId;
            }

            ISecureStorage storage = services.GetRequiredService<ISecureStorage>();
            ITwitchOAuthService oauth = services.GetRequiredService<ITwitchOAuthService>();

            TwitchTokens? broadcasterToken = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
            if (broadcasterToken is null)
            {
                return null;
            }

            TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(broadcasterToken.AccessToken, ct);
            if (validation is null)
            {
                _logger.LogInformation("Broadcaster token expired — refreshing for announcement");
                TwitchTokens refreshed = await oauth.RefreshTokenAsync(broadcasterToken.RefreshToken, ct);
                await storage.SaveTokensAsync(TokenType.Broadcaster, refreshed, ct);
                validation = await oauth.ValidateTokenAsync(refreshed.AccessToken, ct);
            }

            _cachedBroadcasterId = validation?.UserId;
            return _cachedBroadcasterId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve broadcaster ID");
            return null;
        }
        finally
        {
            _broadcasterIdLock.Release();
        }
    }
}
