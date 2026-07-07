using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Batches per-message user stat updates (message count, last seen, display name, role sync)
/// and flushes to the database every 30 seconds instead of per-message.
///
/// Also implements <see cref="ISessionStatsCollector"/>: accumulates per-session analytics
/// (unique chatters, total messages, new followers, new subscribers) as a side effect of
/// the existing Enqueue() call. These counters are read and reset by StreamAnalyticsService
/// when a stream session closes — no additional per-message call required.
/// </summary>
public class UserStatsBatcher : BackgroundService, ISessionStatsCollector
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserStatsBatcher> _logger;

    /// <summary>
    /// Pending stat updates keyed by TwitchId. Each entry accumulates message count
    /// and records the latest metadata.
    /// </summary>
    private readonly ConcurrentDictionary<string, PendingUserUpdate> _pending = new();

    /// <summary>
    /// Tracks which users have had their follower status checked this session.
    /// Reset on bot restart. Prevents redundant Helix API calls.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _followerChecked = new();

    // ─── Per-session analytics counters ──────────────────────────
    // Accumulated by Enqueue() (messages/chatters) and RecordFollow/RecordSubscription
    // (EventSub events). Read and reset by StreamAnalyticsService via GetAndResetStats().

    private readonly object _sessionLock = new();
    private readonly HashSet<string> _sessionUniqueChatters = new();
    private int _sessionMessageCount;
    private int _sessionNewFollowers;
    private int _sessionNewSubscribers;

    /// <summary>
    /// Initializes a new instance of <see cref="UserStatsBatcher"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public UserStatsBatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<UserStatsBatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Enqueues a user stat update. Called from ChatMessagePipeline for every message.
    /// Thread-safe, lock-free (ConcurrentDictionary).
    /// </summary>
    /// <param name="message">The chat message whose author's stats should be queued.</param>
    public void Enqueue(ChatMessage message)
    {
        _pending.AddOrUpdate(
            message.UserId,
            _ => new PendingUserUpdate
            {
                TwitchId = message.UserId,
                Username = message.Username,
                DisplayName = message.DisplayName,
                IsMod = message.IsModerator,
                IsSubscriber = message.IsSubscriber,
                IsBroadcaster = message.IsBroadcaster,
                MessageIncrement = 1,
                LastSeenAt = DateTimeOffset.UtcNow
            },
            (_, existing) =>
            {
                existing.MessageIncrement++;
                existing.DisplayName = message.DisplayName;
                existing.IsMod = message.IsModerator;
                existing.IsSubscriber = message.IsSubscriber;
                existing.IsBroadcaster = message.IsBroadcaster;
                existing.LastSeenAt = DateTimeOffset.UtcNow;
                return existing;
            });

        // Accumulate per-session analytics — same data, no extra call from the pipeline.
        lock (_sessionLock)
        {
            _sessionUniqueChatters.Add(message.UserId);
            _sessionMessageCount++;
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserStatsBatcher starting — flush interval 30 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await FlushAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error flushing user stats batch");
            }
        }

        // Final flush on shutdown
        try
        {
            await FlushAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Final flush failed during shutdown");
        }
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        if (_pending.IsEmpty)
        {
            return;
        }

        List<PendingUserUpdate> batch = new();
        foreach (string key in _pending.Keys)
        {
            if (_pending.TryRemove(key, out PendingUserUpdate? update))
            {
                batch.Add(update);
            }
        }

        if (batch.Count == 0)
        {
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        int processed = 0;
        foreach (PendingUserUpdate update in batch)
        {
            try
            {
                User user = await users.GetOrCreateAsync(update.TwitchId, update.Username, ct);
                user.MessageCount += update.MessageIncrement;
                user.LastSeenAt = update.LastSeenAt;
                user.DisplayName = update.DisplayName;
                user.IsMod = update.IsMod;
                user.IsSubscriber = update.IsSubscriber;
                user.IsBroadcaster = update.IsBroadcaster;
                await users.UpdateAsync(user, ct);
                processed++;

                // Check follower status once per session for users without FollowDate
                if (!user.FollowDate.HasValue && _followerChecked.TryAdd(user.TwitchId, true))
                {
                    try
                    {
                        IBroadcasterHelixClient broadcasterHelix =
                            scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
                        ISecureStorage storage = scope.ServiceProvider.GetRequiredService<ISecureStorage>();
                        ITwitchOAuthService oauth = scope.ServiceProvider.GetRequiredService<ITwitchOAuthService>();

                        TwitchTokens? broadcasterTokens = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
                        if (broadcasterTokens is not null)
                        {
                            TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(
                                broadcasterTokens.AccessToken, ct);
                            if (validation is not null)
                            {
                                if (!Array.Exists(validation.Scopes, s => string.Equals(s, "moderator:read:followers", StringComparison.Ordinal)))
                                {
                                    _logger.LogWarning(
                                        "Broadcaster token is missing the moderator:read:followers scope — follow dates cannot be retrieved. Re-authorize the broadcaster account.");
                                }
                                else
                                {
                                    DateTimeOffset? followedAt = await broadcasterHelix.GetFollowedAtAsync(
                                        validation.UserId, user.TwitchId, ct);

                                    if (followedAt.HasValue)
                                    {
                                        user.FollowDate = followedAt.Value;
                                        await users.UpdateAsync(user, ct);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Follower check failed for {TwitchId}", user.TwitchId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to flush stats for user {TwitchId}", update.TwitchId);
            }
        }

        _logger.LogDebug("Flushed user stats batch: {Count} users updated", processed);
    }

    // ─── ISessionStatsCollector ──────────────────────────────────

    /// <inheritdoc />
    public void RecordChatMessage(string userId)
    {
        // Not called externally — session chat stats are accumulated inside Enqueue().
        // Exists to satisfy the interface contract. Direct callers should use Enqueue(ChatMessage).
        lock (_sessionLock)
        {
            _sessionUniqueChatters.Add(userId);
            _sessionMessageCount++;
        }
    }

    /// <inheritdoc />
    public void RecordFollow()
    {
        Interlocked.Increment(ref _sessionNewFollowers);
    }

    /// <inheritdoc />
    public void RecordSubscription(int count = 1)
    {
        Interlocked.Add(ref _sessionNewSubscribers, count);
    }

    /// <inheritdoc />
    public SessionStats GetAndResetStats()
    {
        lock (_sessionLock)
        {
            SessionStats stats = new()
            {
                UniqueChatters = _sessionUniqueChatters.Count,
                TotalMessages = _sessionMessageCount,
                NewFollowers = Interlocked.Exchange(ref _sessionNewFollowers, 0),
                NewSubscribers = Interlocked.Exchange(ref _sessionNewSubscribers, 0),
            };

            _sessionUniqueChatters.Clear();
            _sessionMessageCount = 0;

            return stats;
        }
    }

    /// <summary>Represents accumulated stat updates for a single user within a batch window.</summary>
    private sealed class PendingUserUpdate
    {
        public string TwitchId { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsMod { get; set; }
        public bool IsSubscriber { get; set; }
        public bool IsBroadcaster { get; set; }
        public int MessageIncrement { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }
    }
}
