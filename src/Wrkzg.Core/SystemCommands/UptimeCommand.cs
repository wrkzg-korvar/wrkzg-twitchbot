using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Shows how long the current stream has been live.
/// </summary>
public class UptimeCommand : ISystemCommand
{
    public string Trigger => "!uptime";
    public string[] Aliases => new[] { "!live" };
    public string Description => "Shows how long the stream has been live.";
    public string? DefaultResponseTemplate => "Stream has been live for {uptime}.";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;

    public UptimeCommand(IServiceScopeFactory scopeFactory, ITwitchChatClient chatClient)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        string? channel = _chatClient.JoinedChannel;
        if (channel is null)
        {
            return "The stream is currently offline.";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        ITwitchHelixClient helix = scope.ServiceProvider.GetRequiredService<ITwitchHelixClient>();

        StreamInfo? stream = await helix.GetStreamAsync(channel, ct);
        if (stream is null)
        {
            return "The stream is currently offline.";
        }

        if (!DateTimeOffset.TryParse(stream.StartedAt, out DateTimeOffset startedAt))
        {
            return "The stream is currently offline.";
        }

        TimeSpan duration = DateTimeOffset.UtcNow - startedAt;
        string formatted = FormatUptime(duration);

        return $"Stream has been live for {formatted}.";
    }

    /// <summary>
    /// Formats a duration for display:
    ///   less than 1 minute  → "less than a minute"
    ///   less than 1 hour    → "45m 12s"
    ///   1+ hours            → "2h 15m"
    ///   24+ hours           → "1d 3h 15m"
    /// </summary>
    internal static string FormatUptime(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
        {
            return "less than a minute";
        }

        int totalMinutes = (int)duration.TotalMinutes;
        int days = totalMinutes / (60 * 24);
        int hours = (totalMinutes % (60 * 24)) / 60;
        int minutes = totalMinutes % 60;
        int seconds = duration.Seconds;

        if (days > 0)
        {
            return minutes > 0
                ? $"{days}d {hours}h {minutes}m"
                : hours > 0
                    ? $"{days}d {hours}h"
                    : $"{days}d";
        }

        if (hours > 0)
        {
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
    }
}
