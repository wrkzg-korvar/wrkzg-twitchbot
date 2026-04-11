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
    private readonly ITwitchChatClient _chat;
    private readonly ILogger<TimedMessageService> _logger;
    private int _chatLinesSinceLastCheck;

    private string? _cachedBroadcasterId;
    private readonly SemaphoreSlim _broadcasterIdLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of <see cref="TimedMessageService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="chat">The Twitch IRC chat client for sending timed messages.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public TimedMessageService(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chat,
        ILogger<TimedMessageService> logger)
    {
        _scopeFactory = scopeFactory;
        _chat = chat;
        _logger = logger;
    }

    /// <summary>Called by ChatMessagePipeline for every chat message to track activity.</summary>
    public void IncrementChatLineCounter()
    {
        Interlocked.Increment(ref _chatLinesSinceLastCheck);
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

    private async Task CheckAndFireTimersAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ITimedMessageRepository repo = scope.ServiceProvider.GetRequiredService<ITimedMessageRepository>();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        IBroadcasterHelixClient broadcasterHelix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
        IBotHelixClient botHelix = scope.ServiceProvider.GetRequiredService<IBotHelixClient>();

        string? channelName = await settings.GetAsync("Bot.Channel", ct);
        bool isLive = false;
        string? broadcasterId = null;

        if (!string.IsNullOrWhiteSpace(channelName))
        {
            try
            {
                StreamInfo? stream = await broadcasterHelix.GetStreamAsync(channelName, ct);
                isLive = stream is not null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to check stream status for timed messages");
            }

            // Resolve broadcaster ID from token (cached, thread-safe)
            try
            {
                broadcasterId = await ResolveBroadcasterIdAsync(scope.ServiceProvider, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to resolve broadcaster ID for announcements");
            }
        }

        IReadOnlyList<Models.TimedMessage> timers = await repo.GetEnabledAsync(ct);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int chatLines = Interlocked.Exchange(ref _chatLinesSinceLastCheck, 0);

        foreach (Models.TimedMessage timer in timers)
        {
            if (isLive && !timer.RunWhenOnline)
            {
                continue;
            }
            if (!isLive && !timer.RunWhenOffline)
            {
                continue;
            }

            if (timer.LastFiredAt.HasValue)
            {
                double minutesSinceLastFire = (now - timer.LastFiredAt.Value).TotalMinutes;
                if (minutesSinceLastFire < timer.IntervalMinutes)
                {
                    continue;
                }
            }

            if (timer.MinChatLines > 0 && chatLines < timer.MinChatLines)
            {
                continue;
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

            timer.NextMessageIndex = (timer.NextMessageIndex + 1) % timer.Messages.Length;
            timer.LastFiredAt = now;
            await repo.UpdateAsync(timer, ct);

            _logger.LogInformation("Fired timed message '{Name}': {Message}",
                timer.Name, message.Length > 60 ? message[..60] + "\u2026" : message);
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
