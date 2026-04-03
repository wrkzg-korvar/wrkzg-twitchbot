using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.ChatGames;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Services;

/// <summary>
/// Manages the song request queue: add, skip, play next, metadata fetching.
/// Singleton service — coordinates queue state across chat commands and API.
/// </summary>
public class SongRequestService : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ILogger<SongRequestService> _logger;
    private readonly HttpClient _http;
    private readonly GameMessageTemplates _msg;

    private static readonly Regex YoutubeUrlPattern = new(
        @"(?:youtube\.com/watch\?v=|youtu\.be/|youtube\.com/embed/|youtube\.com/v/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    private int _maxDuration = 600;
    private int _maxPerUser = 3;
    private int _pointsCost;
    private bool _queueOpen;

    private static readonly Dictionary<string, string> DefaultMessages = new()
    {
        ["QueueClosed"] = "Song request queue is currently closed.",
        ["InvalidUrl"] = "Invalid YouTube URL. Usage: !sr <YouTube URL>",
        ["AlreadyInQueue"] = "That song is already in the queue!",
        ["UserLimit"] = "You already have {max} songs in the queue!",
        ["VideoNotFound"] = "Could not find that video. Check the URL and try again.",
        ["NotEnoughPoints"] = "You need {cost} points to request a song!",
        ["Added"] = "Added: '{title}' — Position #{position}",
        ["NothingPlaying"] = "No song is currently playing.",
        ["Skipped"] = "Skipped: '{title}'",
        ["QueueEmpty"] = "The song queue is empty.",
        ["NowPlaying"] = "Now playing: {title} [requested by {user}]",
        ["QueueOpened"] = "Song request queue is now open!",
        ["QueueClosedMsg"] = "Song request queue is now closed.",
        ["Usage"] = "Usage: !sr <YouTube URL>",
    };

    public SongRequestService(
        IServiceScopeFactory scopeFactory,
        IChatEventBroadcaster broadcaster,
        ILogger<SongRequestService> logger)
    {
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _logger = logger;
        _msg = new GameMessageTemplates("SongRequest", DefaultMessages);
    }

    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string? val = await settings.GetAsync("SongRequest.MaxDuration", ct);
            if (val is not null && int.TryParse(val, out int md)) { _maxDuration = md; }

            val = await settings.GetAsync("SongRequest.MaxPerUser", ct);
            if (val is not null && int.TryParse(val, out int mu)) { _maxPerUser = mu; }

            val = await settings.GetAsync("SongRequest.PointsCost", ct);
            if (val is not null && int.TryParse(val, out int pc)) { _pointsCost = pc; }

            val = await settings.GetAsync("SongRequest.QueueOpen", ct);
            if (val is not null) { _queueOpen = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase); }

            await _msg.LoadAsync(_scopeFactory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load SongRequest settings");
        }
    }

    public async Task<string> RequestSongAsync(string input, string requestedBy, CancellationToken ct = default)
    {
        await LoadSettingsAsync(ct);

        if (!_queueOpen)
        {
            return _msg.Get("QueueClosed");
        }

        string? videoId = ExtractVideoId(input);
        if (videoId is null)
        {
            return _msg.Get("InvalidUrl");
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        ISongRequestRepository repo = scope.ServiceProvider.GetRequiredService<ISongRequestRepository>();

        if (await repo.IsVideoInQueueAsync(videoId, ct))
        {
            return _msg.Get("AlreadyInQueue");
        }

        int userCount = await repo.GetUserQueueCountAsync(requestedBy, ct);
        if (userCount >= _maxPerUser)
        {
            return _msg.Get("UserLimit", ("max", _maxPerUser.ToString()));
        }

        (string title, string? thumbnail) = await FetchVideoMetadataAsync(videoId, ct);
        if (string.IsNullOrWhiteSpace(title))
        {
            return _msg.Get("VideoNotFound");
        }

        if (_pointsCost > 0)
        {
            IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            User? user = await users.GetByTwitchIdAsync(requestedBy, ct);
            if (user is null || user.Points < _pointsCost)
            {
                return _msg.Get("NotEnoughPoints", ("cost", _pointsCost.ToString()));
            }
            user.Points -= _pointsCost;
            await users.UpdateAsync(user, ct);
        }

        SongRequest request = new()
        {
            VideoId = videoId,
            Title = title,
            ThumbnailUrl = thumbnail,
            RequestedBy = requestedBy,
            PointsCost = _pointsCost > 0 ? _pointsCost : null,
        };

        request = await repo.CreateAsync(request, ct);
        int position = await repo.GetQueueCountAsync(ct);

        await _broadcaster.BroadcastSongQueueUpdatedAsync(ct);

        return _msg.Get("Added", ("title", title), ("position", position.ToString()));
    }

    public async Task<string> SkipCurrentAsync(CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISongRequestRepository repo = scope.ServiceProvider.GetRequiredService<ISongRequestRepository>();

        SongRequest? current = await repo.GetCurrentlyPlayingAsync(ct);
        if (current is null)
        {
            return _msg.Get("NothingPlaying");
        }

        current.Status = SongRequestStatus.Skipped;
        current.PlayedAt = DateTimeOffset.UtcNow;
        await repo.UpdateAsync(current, ct);
        await _broadcaster.BroadcastSongQueueUpdatedAsync(ct);

        return _msg.Get("Skipped", ("title", current.Title));
    }

    public async Task<SongRequest?> PlayNextAsync(CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISongRequestRepository repo = scope.ServiceProvider.GetRequiredService<ISongRequestRepository>();

        SongRequest? current = await repo.GetCurrentlyPlayingAsync(ct);
        if (current is not null)
        {
            current.Status = SongRequestStatus.Played;
            current.PlayedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(current, ct);
        }

        SongRequest? next = await repo.GetNextInQueueAsync(ct);
        if (next is not null)
        {
            next.Status = SongRequestStatus.Playing;
            next.PlayedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(next, ct);
        }

        await _broadcaster.BroadcastSongQueueUpdatedAsync(ct);
        return next;
    }

    public bool IsQueueOpen => _queueOpen;
    public int MaxPerUser => _maxPerUser;
    public int MaxDuration => _maxDuration;
    public int PointsCost => _pointsCost;

    public async Task SetQueueOpenAsync(bool open, CancellationToken ct = default)
    {
        _queueOpen = open;
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        await settings.SetAsync("SongRequest.QueueOpen", open.ToString().ToLowerInvariant(), ct);
    }

    public Dictionary<string, string> GetMessageTemplates() => _msg.GetAll();
    public Dictionary<string, string> GetDefaultMessageTemplates() => _msg.GetDefaults();

    internal static string? ExtractVideoId(string input)
    {
        input = input.Trim();

        Match match = YoutubeUrlPattern.Match(input);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        if (input.Length == 11 && Regex.IsMatch(input, @"^[a-zA-Z0-9_-]+$"))
        {
            return input;
        }

        return null;
    }

    private async Task<(string title, string? thumbnail)> FetchVideoMetadataAsync(string videoId, CancellationToken ct)
    {
        try
        {
            string url = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";
            HttpResponseMessage response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return (string.Empty, null);
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            using JsonDocument doc = JsonDocument.Parse(json);

            string title = doc.RootElement.TryGetProperty("title", out JsonElement titleEl)
                ? titleEl.GetString() ?? ""
                : "";
            string? thumbnail = doc.RootElement.TryGetProperty("thumbnail_url", out JsonElement thumbEl)
                ? thumbEl.GetString()
                : null;

            return (title, thumbnail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch YouTube metadata for {VideoId}", videoId);
            return (string.Empty, null);
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
}
