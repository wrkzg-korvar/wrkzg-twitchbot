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
/// Rationale: The ChatMessagePipeline previously created a new IServiceScope + DB write for
/// every single chat message. At 50+ messages/minute, that's 50+ DB writes/minute just for
/// user stats. This batcher reduces that to 1 batch write every 30 seconds.
/// </summary>
public class UserStatsBatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserStatsBatcher> _logger;

    /// <summary>
    /// Pending stat updates keyed by TwitchId. Each entry accumulates message count
    /// and records the latest metadata.
    /// </summary>
    private readonly ConcurrentDictionary<string, PendingUserUpdate> _pending = new();

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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to flush stats for user {TwitchId}", update.TwitchId);
            }
        }

        _logger.LogDebug("Flushed user stats batch: {Count} users updated", processed);
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
