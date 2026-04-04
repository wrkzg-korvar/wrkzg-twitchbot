using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

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
        ITwitchHelixClient helix = scope.ServiceProvider.GetRequiredService<ITwitchHelixClient>();

        string? channelName = await settings.GetAsync("channelName", ct);
        bool isLive = false;
        if (channelName is not null)
        {
            try
            {
                StreamInfo? stream = await helix.GetStreamAsync(channelName, ct);
                isLive = stream is not null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to check stream status for timed messages");
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
                await _chat.SendMessageAsync($"/announce {message}", ct);
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
}
