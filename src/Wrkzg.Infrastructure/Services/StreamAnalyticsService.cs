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
/// Background service that polls the Twitch Helix API every 60 seconds
/// to track stream sessions, viewer counts, and category changes.
/// </summary>
public class StreamAnalyticsService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StreamAnalyticsService> _logger;

    private Timer? _timer;
    private StreamSession? _currentSession;
    private CategorySegment? _currentSegment;
    private string? _channelLogin;
    private bool _isPolling;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAnalyticsService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers to access repositories.</param>
    /// <param name="logger">The logger for analytics polling diagnostics.</param>
    public StreamAnalyticsService(
        IServiceScopeFactory scopeFactory,
        ILogger<StreamAnalyticsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Starts the analytics polling timer and resumes any unclosed session from a previous crash.</summary>
    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("StreamAnalyticsService starting");

        // Load channel name from settings
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            _channelLogin = await settings.GetAsync("Bot.Channel", ct);

            // Check for any unclosed session from a previous crash
            IStreamAnalyticsRepository repo = scope.ServiceProvider.GetRequiredService<IStreamAnalyticsRepository>();
            StreamSession? activeSession = await repo.GetActiveSessionAsync(ct);
            if (activeSession is not null)
            {
                _currentSession = activeSession;
                _currentSegment = activeSession.CategorySegments
                    .FirstOrDefault(s => s.EndedAt is null);
                _logger.LogInformation("Resuming active session {SessionId}", activeSession.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize StreamAnalyticsService");
        }

        // Poll every 60 seconds
        _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
    }

    /// <summary>Stops the analytics polling timer.</summary>
    public Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("StreamAnalyticsService stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
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
        if (string.IsNullOrWhiteSpace(_channelLogin))
        {
            // Try to load channel name (may have been set after startup)
            using IServiceScope settingsScope = _scopeFactory.CreateScope();
            ISettingsRepository settingsRepo = settingsScope.ServiceProvider
                .GetRequiredService<ISettingsRepository>();
            _channelLogin = await settingsRepo.GetAsync("Bot.Channel");
            if (string.IsNullOrWhiteSpace(_channelLogin))
            {
                return;
            }
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        ITwitchHelixClient helix = scope.ServiceProvider.GetRequiredService<ITwitchHelixClient>();
        IStreamAnalyticsRepository repo = scope.ServiceProvider.GetRequiredService<IStreamAnalyticsRepository>();

        StreamInfo? stream = await helix.GetStreamAsync(_channelLogin);

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
        // Create session if not exists
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

        // Add viewer snapshot
        await repo.AddSnapshotAsync(new ViewerSnapshot
        {
            StreamSessionId = _currentSession.Id,
            ViewerCount = stream.ViewerCount,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Update peak viewers
        if (stream.ViewerCount > _currentSession.PeakViewers)
        {
            _currentSession.PeakViewers = stream.ViewerCount;
            await repo.UpdateSessionAsync(_currentSession);
        }

        // Check for category change
        string currentCategory = !string.IsNullOrWhiteSpace(stream.GameName)
            ? stream.GameName
            : "Unknown";

        if (_currentSegment is null || !string.Equals(_currentSegment.CategoryName, currentCategory, StringComparison.Ordinal))
        {
            // Close old segment
            if (_currentSegment is not null)
            {
                _currentSegment.EndedAt = DateTimeOffset.UtcNow;
                _currentSegment.DurationMinutes = (int)(DateTimeOffset.UtcNow - _currentSegment.StartedAt).TotalMinutes;
                await repo.UpdateSegmentAsync(_currentSegment);
                _logger.LogInformation("Category segment ended: {Category} ({Duration}m)",
                    _currentSegment.CategoryName, _currentSegment.DurationMinutes);
            }

            // Open new segment
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
        // Close current segment
        if (_currentSegment is not null)
        {
            _currentSegment.EndedAt = DateTimeOffset.UtcNow;
            _currentSegment.DurationMinutes = (int)(DateTimeOffset.UtcNow - _currentSegment.StartedAt).TotalMinutes;
            await repo.UpdateSegmentAsync(_currentSegment);
        }

        // Close session
        _currentSession!.EndedAt = DateTimeOffset.UtcNow;
        _currentSession.DurationMinutes = (int)(DateTimeOffset.UtcNow - _currentSession.StartedAt).TotalMinutes;

        // Calculate average viewers from snapshots
        System.Collections.Generic.IReadOnlyList<ViewerSnapshot> snapshots =
            await repo.GetSnapshotsForSessionAsync(_currentSession.Id);
        if (snapshots.Count > 0)
        {
            _currentSession.AverageViewers = snapshots.Average(s => s.ViewerCount);
        }

        await repo.UpdateSessionAsync(_currentSession);
        _logger.LogInformation("Stream session ended: {Duration}m, Peak: {Peak}, Avg: {Avg:F1}",
            _currentSession.DurationMinutes, _currentSession.PeakViewers, _currentSession.AverageViewers);

        _currentSession = null;
        _currentSegment = null;
    }
}
