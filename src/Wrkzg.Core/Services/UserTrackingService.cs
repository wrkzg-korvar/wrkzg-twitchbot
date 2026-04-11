using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services
#pragma warning disable CA1873 // Avoid potentially expensive call

namespace Wrkzg.Core.Services;

/// <summary>
/// Background service that runs every 60 seconds while the app is active.
///
/// Each tick:
///   1. Check if the stream is live (via Helix API)
///   2. If live: award points and increment watch time for active users
///   3. Broadcast viewer count to dashboard via SignalR
///
/// "Active users" are tracked by the TwitchChatClient — anyone who sent
/// a message or joined the channel in the last 5 minutes is considered active.
///
/// Uses IServiceScopeFactory for scoped DB access from this Singleton service.
/// </summary>
public class UserTrackingService : IUserTrackingService, IDisposable
{
    private readonly IBroadcasterHelixClient _helix;
    private readonly ITwitchChatClient _chatClient;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserTrackingService> _logger;

    private Timer? _timer;

    /// <summary>
    /// Tracks recently active users (sent a message within the last 5 minutes).
    /// Key: TwitchId. Populated by ChatMessagePipeline when processing messages.
    /// </summary>
    private readonly Dictionary<string, DateTimeOffset> _recentlyActiveUsers = new();
    private readonly object _activeUsersLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="UserTrackingService"/>.
    /// </summary>
    /// <param name="helix">The Twitch Helix API client for checking stream live status.</param>
    /// <param name="chatClient">The Twitch IRC chat client for checking connection state.</param>
    /// <param name="broadcaster">Broadcasts viewer count updates to the dashboard.</param>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public UserTrackingService(
        IBroadcasterHelixClient helix,
        ITwitchChatClient chatClient,
        IChatEventBroadcaster broadcaster,
        IServiceScopeFactory scopeFactory,
        ILogger<UserTrackingService> logger)
    {
        _helix = helix;
        _chatClient = chatClient;
        _broadcaster = broadcaster;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Starts the 60-second polling timer for user tracking and point awards.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for startup.</param>
    /// <returns>A completed task once the timer is started.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("UserTrackingService starting — polling every 60 seconds");

        // Start polling every 60 seconds
        _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the polling timer and releases resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for shutdown.</param>
    /// <returns>A completed task once the timer is disposed.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("UserTrackingService stopping");
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called by ChatMessagePipeline to mark a user as active.
    /// </summary>
    public void MarkUserActive(string twitchId)
    {
        lock (_activeUsersLock)
        {
            _recentlyActiveUsers[twitchId] = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Timer callback — runs every 60 seconds.
    /// </summary>
    private async void OnTick(object? state)
    {
        try
        {
            await TickAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserTracking tick");
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        // Only track if bot is connected
        if (!_chatClient.IsConnected || _chatClient.JoinedChannel is null)
        {
            return;
        }

        string channel = _chatClient.JoinedChannel;

        // Check if stream is live
        StreamInfo? stream = await _helix.GetStreamAsync(channel, ct);

        if (stream is null)
        {
            // Stream is offline — no points, no watch time
            return;
        }

        // Broadcast viewer count to dashboard
        await _broadcaster.BroadcastViewerCountAsync(stream.ViewerCount, ct);

        // Get recently active users (last 5 minutes)
        List<string> activeUserIds = GetAndCleanActiveUsers();

        if (activeUserIds.Count == 0)
        {
            return;
        }

        // Award points and watch time
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

        // Read settings
        string? pointsPerMinuteStr = await settings.GetAsync("Points.PerMinute", ct);
        string? subMultiplierStr = await settings.GetAsync("Points.SubMultiplier", ct);

        int pointsPerMinute = int.TryParse(pointsPerMinuteStr, CultureInfo.InvariantCulture, out int ppm) ? ppm : 10;
        double subMultiplier = double.TryParse(subMultiplierStr, CultureInfo.InvariantCulture, out double sm) ? sm : 1.5;

        int usersRewarded = 0;

        foreach (string twitchId in activeUserIds)
        {
            User? user = await users.GetByTwitchIdAsync(twitchId, ct);
            if (user is null || user.IsBanned)
            {
                continue;
            }

            // Calculate points
            long points = user.IsSubscriber
                ? (long)(pointsPerMinute * subMultiplier)
                : pointsPerMinute;

            user.Points += points;
            user.WatchedMinutes += 1;

            await users.UpdateAsync(user, ct);
            usersRewarded++;
        }

        if (usersRewarded > 0)
        {
            _logger.LogDebug(
                "Awarded {Points} points to {Count} active users (stream: {Channel})",
                pointsPerMinute, usersRewarded, channel);
        }
    }

    /// <summary>
    /// Returns TwitchIds of users active in the last 5 minutes and removes expired entries.
    /// </summary>
    private List<string> GetAndCleanActiveUsers()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        List<string> active = new();

        lock (_activeUsersLock)
        {
            List<string> expired = new();

            foreach (KeyValuePair<string, DateTimeOffset> kvp in _recentlyActiveUsers)
            {
                if (kvp.Value >= cutoff)
                {
                    active.Add(kvp.Key);
                }
                else
                {
                    expired.Add(kvp.Key);
                }
            }

            foreach (string key in expired)
            {
                _recentlyActiveUsers.Remove(key);
            }
        }

        return active;
    }
}
