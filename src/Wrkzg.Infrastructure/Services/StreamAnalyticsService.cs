using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Services;

/// <summary>
/// Background service that polls the cached <see cref="IStreamStatusProvider"/>
/// every 60 seconds to track stream sessions, viewer counts, and category changes.
/// </summary>
public class StreamAnalyticsService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStreamStatusProvider _streamStatus;
    private readonly ISessionStatsCollector _sessionStats;
    private readonly ILogger<StreamAnalyticsService> _logger;

    private Timer? _timer;
    private StreamSession? _currentSession;
    private CategorySegment? _currentSegment;
    private bool _isPolling;
    private int _segmentViewerSum;
    private int _segmentViewerCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAnalyticsService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers to access repositories.</param>
    /// <param name="streamStatus">Cached stream live status provider — replaces direct Helix polling.</param>
    /// <param name="sessionStats">Collector for per-session chat/follow/sub statistics.</param>
    /// <param name="logger">The logger for analytics polling diagnostics.</param>
    public StreamAnalyticsService(
        IServiceScopeFactory scopeFactory,
        IStreamStatusProvider streamStatus,
        ISessionStatsCollector sessionStats,
        ILogger<StreamAnalyticsService> logger)
    {
        _scopeFactory = scopeFactory;
        _streamStatus = streamStatus;
        _sessionStats = sessionStats;
        _logger = logger;
    }

    /// <summary>Starts the analytics polling timer and resolves any unclosed session from a previous run.</summary>
    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("StreamAnalyticsService starting");

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IStreamAnalyticsRepository repo = scope.ServiceProvider.GetRequiredService<IStreamAnalyticsRepository>();
            StreamSession? activeSession = await repo.GetActiveSessionAsync(ct);

            if (activeSession is not null)
            {
                TimeSpan elapsed = DateTimeOffset.UtcNow - activeSession.StartedAt;

                if (elapsed.TotalHours > 48)
                {
                    // Session has been open for more than 48 hours — this is a phantom session
                    // from a previous crash or missed offline detection. Close it using the last
                    // known viewer snapshot timestamp as EndedAt (best available approximation
                    // of when the stream actually ended).
                    System.Collections.Generic.IReadOnlyList<ViewerSnapshot> snapshots =
                        await repo.GetSnapshotsForSessionAsync(activeSession.Id, ct);

                    DateTimeOffset estimatedEnd = snapshots.Count > 0
                        ? snapshots[snapshots.Count - 1].Timestamp
                        : activeSession.StartedAt.AddHours(12); // fallback: 12h max

                    activeSession.EndedAt = estimatedEnd;
                    activeSession.DurationMinutes = (int)(estimatedEnd - activeSession.StartedAt).TotalMinutes;

                    if (snapshots.Count > 0)
                    {
                        activeSession.AverageViewers = snapshots.Average(s => s.ViewerCount);
                    }

                    CategorySegment? openSegment = activeSession.CategorySegments
                        .FirstOrDefault(s => s.EndedAt is null);
                    if (openSegment is not null)
                    {
                        openSegment.EndedAt = estimatedEnd;
                        openSegment.DurationMinutes = (int)(estimatedEnd - openSegment.StartedAt).TotalMinutes;
                        await repo.UpdateSegmentAsync(openSegment, ct);
                    }

                    await repo.UpdateSessionAsync(activeSession, ct);

                    // Discard any accumulated stats from a stale session — they're unreliable.
                    _sessionStats.GetAndResetStats();

                    _logger.LogWarning(
                        "Closed stale session {SessionId} (started {StartedAt}, open for {Hours:F1}h). " +
                        "Estimated end from last snapshot: {EstimatedEnd}",
                        activeSession.Id, activeSession.StartedAt, elapsed.TotalHours, estimatedEnd);

                    _currentSession = null;
                    _currentSegment = null;
                }
                else
                {
                    _currentSession = activeSession;
                    _currentSegment = activeSession.CategorySegments
                        .FirstOrDefault(s => s.EndedAt is null);
                    _logger.LogInformation("Resuming active session {SessionId} (started {StartedAt})",
                        activeSession.Id, activeSession.StartedAt);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize StreamAnalyticsService");
        }

        _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60));
    }

    /// <summary>Stops the analytics polling timer and closes any active session (in-memory and database).</summary>
    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("StreamAnalyticsService stopping");
        _timer?.Change(Timeout.Infinite, 0);

        // 1. Close the in-memory tracked session (normal path).
        if (_currentSession is not null)
        {
            int sessionId = _currentSession.Id;
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IStreamAnalyticsRepository repo = scope.ServiceProvider.GetRequiredService<IStreamAnalyticsRepository>();
                await HandleStreamOfflineAsync(repo);
                _logger.LogInformation("Closed active session {SessionId} on shutdown", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close in-memory session {SessionId} on shutdown", sessionId);
            }
        }

        // 2. Safety net: check the database for any lingering unclosed session.
        //    Catches edge cases where _currentSession is null but the DB still has
        //    a session with EndedAt == null (e.g. exception during polling lost the
        //    in-memory reference, or a previous run left an orphan).
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IStreamAnalyticsRepository repo = scope.ServiceProvider.GetRequiredService<IStreamAnalyticsRepository>();
            StreamSession? dbSession = await repo.GetActiveSessionAsync(ct);

            if (dbSession is not null)
            {
                System.Collections.Generic.IReadOnlyList<ViewerSnapshot> snapshots =
                    await repo.GetSnapshotsForSessionAsync(dbSession.Id, ct);

                DateTimeOffset endTime = snapshots.Count > 0
                    ? snapshots[snapshots.Count - 1].Timestamp
                    : DateTimeOffset.UtcNow;

                dbSession.EndedAt = endTime;
                dbSession.DurationMinutes = (int)(endTime - dbSession.StartedAt).TotalMinutes;

                if (snapshots.Count > 0)
                {
                    dbSession.AverageViewers = snapshots.Average(s => s.ViewerCount);
                }

                CategorySegment? openSegment = dbSession.CategorySegments
                    .FirstOrDefault(s => s.EndedAt is null);
                if (openSegment is not null)
                {
                    openSegment.EndedAt = endTime;
                    openSegment.DurationMinutes = (int)(endTime - openSegment.StartedAt).TotalMinutes;
                    await repo.UpdateSegmentAsync(openSegment, ct);
                }

                await repo.UpdateSessionAsync(dbSession, ct);

                // Reset collector to avoid double-counting on next session.
                _sessionStats.GetAndResetStats();

                _logger.LogWarning(
                    "Closed orphaned DB session {SessionId} on shutdown (EndedAt set to {EndTime})",
                    dbSession.Id, endTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for orphaned DB sessions on shutdown");
        }
    }

    /// <summary>Disposes the polling timer.</summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }

    private async void OnTimerTick(object? state)
    {
        if (_isPolling)
        {
            return;
        }

        _isPolling = true;
        try
        {
            await PollStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StreamAnalytics poll error");
        }
        finally
        {
            _isPolling = false;
        }
    }

    private async Task PollStreamAsync()
    {
        // StreamStatusProvider handles channel resolution and Helix polling.
        StreamInfo? stream = _streamStatus.CurrentStream;

        using IServiceScope scope = _scopeFactory.CreateScope();
        IStreamAnalyticsRepository repo = scope.ServiceProvider.GetRequiredService<IStreamAnalyticsRepository>();

        if (stream is not null)
        {
            await HandleStreamLiveAsync(stream, repo);
        }
        else if (_currentSession is not null)
        {
            await HandleStreamOfflineAsync(repo);
        }
    }

    private async Task HandleStreamLiveAsync(StreamInfo stream, IStreamAnalyticsRepository repo)
    {
        // If there's an existing session with a different Twitch stream ID,
        // the previous stream ended and a new one started — close the old session first.
        if (_currentSession is not null
            && !string.IsNullOrWhiteSpace(_currentSession.TwitchStreamId)
            && !string.Equals(_currentSession.TwitchStreamId, stream.Id, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Stream ID changed from {OldId} to {NewId} — closing previous session {SessionId}",
                _currentSession.TwitchStreamId, stream.Id, _currentSession.Id);
            await HandleStreamOfflineAsync(repo);
        }

        if (_currentSession is null)
        {
            _currentSession = await repo.CreateSessionAsync(new StreamSession
            {
                TwitchStreamId = stream.Id,
                StartedAt = DateTimeOffset.UtcNow,
                Title = stream.Title
            });
            _logger.LogInformation("Stream session started: {Title}", stream.Title);
        }

        await repo.AddSnapshotAsync(new ViewerSnapshot
        {
            StreamSessionId = _currentSession.Id,
            ViewerCount = stream.ViewerCount,
            Timestamp = DateTimeOffset.UtcNow
        });

        if (stream.ViewerCount > _currentSession.PeakViewers)
        {
            _currentSession.PeakViewers = stream.ViewerCount;
            await repo.UpdateSessionAsync(_currentSession);
        }

        // Track per-segment viewer stats in memory (no DB write until segment closes).
        if (_currentSegment is not null)
        {
            if (stream.ViewerCount > (_currentSegment.PeakViewers ?? 0))
            {
                _currentSegment.PeakViewers = stream.ViewerCount;
            }

            _segmentViewerSum += stream.ViewerCount;
            _segmentViewerCount++;
        }

        string currentCategory = !string.IsNullOrWhiteSpace(stream.GameName)
            ? stream.GameName
            : "Unknown";

        if (_currentSegment is null || !string.Equals(_currentSegment.CategoryName, currentCategory, StringComparison.Ordinal))
        {
            if (_currentSegment is not null)
            {
                _currentSegment.EndedAt = DateTimeOffset.UtcNow;
                _currentSegment.DurationMinutes = (int)(DateTimeOffset.UtcNow - _currentSegment.StartedAt).TotalMinutes;
                _currentSegment.AverageViewers = _segmentViewerCount > 0
                    ? Math.Round((double)_segmentViewerSum / _segmentViewerCount, 1)
                    : null;
                await repo.UpdateSegmentAsync(_currentSegment);
                _logger.LogInformation("Category segment ended: {Category} ({Duration}m, Avg: {Avg}, Peak: {Peak})",
                    _currentSegment.CategoryName, _currentSegment.DurationMinutes,
                    _currentSegment.AverageViewers, _currentSegment.PeakViewers);
            }

            _segmentViewerSum = 0;
            _segmentViewerCount = 0;

            _currentSegment = await repo.CreateSegmentAsync(new CategorySegment
            {
                StreamSessionId = _currentSession.Id,
                CategoryName = currentCategory,
                TwitchCategoryId = stream.GameId,
                StartedAt = DateTimeOffset.UtcNow
            });
            _logger.LogInformation("Category segment started: {Category}", currentCategory);
        }
    }

    private async Task HandleStreamOfflineAsync(IStreamAnalyticsRepository repo)
    {
        if (_currentSegment is not null)
        {
            _currentSegment.EndedAt = DateTimeOffset.UtcNow;
            _currentSegment.DurationMinutes = (int)(DateTimeOffset.UtcNow - _currentSegment.StartedAt).TotalMinutes;
            _currentSegment.AverageViewers = _segmentViewerCount > 0
                ? Math.Round((double)_segmentViewerSum / _segmentViewerCount, 1)
                : null;
            await repo.UpdateSegmentAsync(_currentSegment);
        }

        _segmentViewerSum = 0;
        _segmentViewerCount = 0;

        _currentSession!.EndedAt = DateTimeOffset.UtcNow;
        _currentSession.DurationMinutes = (int)(DateTimeOffset.UtcNow - _currentSession.StartedAt).TotalMinutes;

        System.Collections.Generic.IReadOnlyList<ViewerSnapshot> snapshots =
            await repo.GetSnapshotsForSessionAsync(_currentSession.Id);
        if (snapshots.Count > 0)
        {
            _currentSession.AverageViewers = snapshots.Average(s => s.ViewerCount);
        }

        // Collect per-session stats from the pipeline and EventSub collectors.
        SessionStats sessionStats = _sessionStats.GetAndResetStats();
        _currentSession.UniqueChatters = sessionStats.UniqueChatters;
        _currentSession.TotalMessages = sessionStats.TotalMessages;
        _currentSession.NewFollowers = sessionStats.NewFollowers;
        _currentSession.NewSubscribers = sessionStats.NewSubscribers;

        await repo.UpdateSessionAsync(_currentSession);
        _logger.LogInformation(
            "Stream session ended: {Duration}m, Peak: {Peak}, Avg: {Avg:F1}, Chatters: {Chatters}, Messages: {Messages}, Follows: {Follows}, Subs: {Subs}",
            _currentSession.DurationMinutes, _currentSession.PeakViewers, _currentSession.AverageViewers,
            _currentSession.UniqueChatters, _currentSession.TotalMessages,
            _currentSession.NewFollowers, _currentSession.NewSubscribers);

        _currentSession = null;
        _currentSegment = null;
    }
}
