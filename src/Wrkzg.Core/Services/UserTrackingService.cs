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
///   1. Read cached stream status from <see cref="IStreamStatusProvider"/>.
///   2. If live: pull all current chatters via Helix Get Chatters and mark them active
///      (covers lurkers who don't send messages).
///   3. Award points and increment watch time for active users.
///   4. Broadcast viewer count to dashboard via SignalR.
///
/// "Active users" combines two sources:
///   - Chatters from Helix Get Chatters (refreshed every 60s, includes lurkers)
///   - Message senders marked via <see cref="MarkUserActive"/> by ChatMessagePipeline
/// </summary>
public class UserTrackingService : IUserTrackingService, IDisposable
{
    private readonly IStreamStatusProvider _streamStatus;
    private readonly IBotHelixClient _botHelix;
    private readonly ITwitchChatClient _chatClient;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserTrackingService> _logger;

    private Timer? _timer;
    private string? _cachedBroadcasterId;

    /// <summary>
    /// Tracks recently active users (chatters from Helix or message senders within the last 5 minutes).
    /// Key: TwitchId.
    /// </summary>
    private readonly Dictionary<string, DateTimeOffset> _recentlyActiveUsers = new();
    private readonly object _activeUsersLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="UserTrackingService"/>.
    /// </summary>
    /// <param name="streamStatus">Cached stream live status provider — replaces direct Helix polling.</param>
    /// <param name="botHelix">Bot-authenticated Helix client used for Get Chatters polling.</param>
    /// <param name="chatClient">The Twitch IRC chat client for checking connection state.</param>
    /// <param name="broadcaster">Broadcasts viewer count updates to the dashboard.</param>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public UserTrackingService(
        IStreamStatusProvider streamStatus,
        IBotHelixClient botHelix,
        ITwitchChatClient chatClient,
        IChatEventBroadcaster broadcaster,
        IServiceScopeFactory scopeFactory,
        ILogger<UserTrackingService> logger)
    {
        _streamStatus = streamStatus;
        _botHelix = botHelix;
        _chatClient = chatClient;
        _broadcaster = broadcaster;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Starts the 60-second polling timer for user tracking and point awards.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("UserTrackingService starting — polling every 60 seconds");

        _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the polling timer and releases resources.
    /// </summary>
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

    /// <inheritdoc />
    public IReadOnlyList<string> GetActiveUserIds()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        List<string> active = new();

        lock (_activeUsersLock)
        {
            foreach (KeyValuePair<string, DateTimeOffset> kvp in _recentlyActiveUsers)
            {
                if (kvp.Value >= cutoff)
                {
                    active.Add(kvp.Key);
                }
            }
        }

        return active;
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
        if (!_chatClient.IsConnected || _chatClient.JoinedChannel is null)
        {
            _logger.LogDebug("UserTracking tick skipped — chat client not connected");
            return;
        }

        if (!_streamStatus.IsLive)
        {
            _logger.LogDebug("UserTracking tick skipped — stream is offline");
            return;
        }

        await _broadcaster.BroadcastViewerCountAsync(_streamStatus.ViewerCount, ct);

        // Resolve broadcaster ID for the Get Chatters endpoint (cached after first lookup).
        if (_cachedBroadcasterId is null)
        {
            _cachedBroadcasterId = await ResolveBroadcasterIdAsync(ct);
            if (_cachedBroadcasterId is null)
            {
                _logger.LogWarning(
                    "Cannot resolve broadcaster ID — falling back to chat-message-only tracking");
            }
        }

        // Pull all current chatters (lurkers + speakers) and mark them active.
        if (_cachedBroadcasterId is not null)
        {
            try
            {
                IReadOnlyList<string> chatters = await _botHelix.GetChattersAsync(_cachedBroadcasterId, ct);
                lock (_activeUsersLock)
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    foreach (string chatterId in chatters)
                    {
                        _recentlyActiveUsers[chatterId] = now;
                    }
                }
                _logger.LogDebug(
                    "Marked {Count} chatters as active from Helix GetChatters", chatters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to poll chatters — using chat-message-only tracking this tick");
            }
        }

        List<string> activeUserIds = GetAndCleanActiveUsers();

        _logger.LogDebug(
            "UserTracking tick — connected: {Connected}, live: {IsLive}, active users: {ActiveCount}",
            _chatClient.IsConnected, _streamStatus.IsLive, activeUserIds.Count);

        if (activeUserIds.Count == 0)
        {
            return;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

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
            _logger.LogInformation(
                "Awarded {Points} points to {Count} active users (chatters + message senders)",
                pointsPerMinute, usersRewarded);
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

    /// <summary>
    /// Resolves the broadcaster's Twitch user ID from the Broadcaster OAuth token.
    /// Cached after first successful resolution.
    /// </summary>
    private async Task<string?> ResolveBroadcasterIdAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISecureStorage storage = scope.ServiceProvider.GetRequiredService<ISecureStorage>();
            ITwitchOAuthService oauth = scope.ServiceProvider.GetRequiredService<ITwitchOAuthService>();

            TwitchTokens? broadcasterToken = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
            if (broadcasterToken is null)
            {
                return null;
            }

            TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(broadcasterToken.AccessToken, ct);
            if (validation is null)
            {
                try
                {
                    TwitchTokens refreshed = await oauth.RefreshTokenAsync(broadcasterToken.RefreshToken, ct);
                    await storage.SaveTokensAsync(TokenType.Broadcaster, refreshed, ct);
                    validation = await oauth.ValidateTokenAsync(refreshed.AccessToken, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh broadcaster token for chatters endpoint");
                    return null;
                }
            }

            return validation?.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve broadcaster ID for chatters endpoint");
            return null;
        }
    }
}
