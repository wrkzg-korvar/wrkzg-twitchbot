using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// !sr — Request a song by YouTube URL.
/// </summary>
public class SongRequestCommand : ISystemCommand
{
    private readonly SongRequestService _songService;

    public string Trigger => "!sr";
    public string[] Aliases => new[] { "!songrequest" };
    public string Description => "Request a song by YouTube URL.";
    public string? DefaultResponseTemplate => null;

    public SongRequestCommand(SongRequestService songService)
    {
        _songService = songService;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        string[] parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return "Usage: !sr <YouTube URL>";
        }

        string input = parts[1].Trim();

        if (string.Equals(input, "open", StringComparison.OrdinalIgnoreCase) && (message.IsModerator || message.IsBroadcaster))
        {
            await _songService.SetQueueOpenAsync(true, ct);
            return "Song request queue is now open!";
        }
        if (string.Equals(input, "close", StringComparison.OrdinalIgnoreCase) && (message.IsModerator || message.IsBroadcaster))
        {
            await _songService.SetQueueOpenAsync(false, ct);
            return "Song request queue is now closed.";
        }

        return await _songService.RequestSongAsync(input, message.DisplayName, ct);
    }
}

/// <summary>
/// !skip — Skip the currently playing song (Mod only).
/// </summary>
public class SkipSongCommand : ISystemCommand
{
    private readonly SongRequestService _songService;

    public string Trigger => "!skip";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Skip the current song.";
    public string? DefaultResponseTemplate => null;

    public SkipSongCommand(SongRequestService songService)
    {
        _songService = songService;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return null;
        }
        return await _songService.SkipCurrentAsync(ct);
    }
}

/// <summary>
/// !queue — Show the next songs in the queue.
/// </summary>
public class QueueCommand : ISystemCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string Trigger => "!queue";
    public string[] Aliases => new[] { "!songlist" };
    public string Description => "Show the next songs in the queue.";
    public string? DefaultResponseTemplate => null;

    public QueueCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISongRequestRepository repo = scope.ServiceProvider.GetRequiredService<ISongRequestRepository>();
        IReadOnlyList<SongRequest> queue = await repo.GetQueueAsync(ct);

        if (queue.Count == 0)
        {
            return "The song queue is empty.";
        }

        List<string> parts = new();
        int shown = 0;
        foreach (SongRequest song in queue)
        {
            if (shown >= 5)
            {
                break;
            }

            string prefix = song.Status == SongRequestStatus.Playing ? "Now: " : $"#{shown + 1}: ";
            parts.Add($"{prefix}{song.Title} [{song.RequestedBy}]");
            shown++;
        }

        int remaining = queue.Count - shown;
        if (remaining > 0)
        {
            parts.Add($"(+{remaining} more)");
        }

        return string.Join(" | ", parts);
    }
}

/// <summary>
/// !currentsong — Show the currently playing song.
/// </summary>
public class CurrentSongCommand : ISystemCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string Trigger => "!currentsong";
    public string[] Aliases => new[] { "!song", "!nowplaying" };
    public string Description => "Show the currently playing song.";
    public string? DefaultResponseTemplate => null;

    public CurrentSongCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISongRequestRepository repo = scope.ServiceProvider.GetRequiredService<ISongRequestRepository>();
        SongRequest? current = await repo.GetCurrentlyPlayingAsync(ct);

        if (current is null)
        {
            return "No song is currently playing.";
        }

        return $"Now playing: {current.Title} [requested by {current.RequestedBy}]";
    }
}
