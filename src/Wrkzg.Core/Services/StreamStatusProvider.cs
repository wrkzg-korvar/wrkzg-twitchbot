using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Singleton background service that polls the Helix API once per minute to determine
/// if the broadcaster's stream is live. All other services read from this cache.
///
/// Rationale: Before this change, UserTrackingService, StreamAnalyticsService, and
/// TimedMessageService each called GetStreamAsync() independently — 4+ API calls/minute
/// for the same data. This provider reduces that to exactly 1 call/minute.
/// </summary>
public class StreamStatusProvider : IStreamStatusProvider, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StreamStatusProvider> _logger;

    private Timer? _timer;
    private volatile StreamInfo? _currentStream;
    private volatile string? _channelLogin;
    private volatile bool _isPolling;
    private int _consecutiveFailures;

    private const int MaxConsecutiveFailuresBeforeOffline = 3;

    /// <inheritdoc />
    public bool IsLive => _currentStream is not null;

    /// <inheritdoc />
    public int ViewerCount => _currentStream?.ViewerCount ?? 0;

    /// <inheritdoc />
    public StreamInfo? CurrentStream => _currentStream;

    /// <inheritdoc />
    public string? ChannelLogin => _channelLogin;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamStatusProvider"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped Helix and settings repositories.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public StreamStatusProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<StreamStatusProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StreamStatusProvider starting — polling every 60 seconds");

        await LoadChannelLoginAsync(cancellationToken);

        _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60));
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StreamStatusProvider stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await PollAsync(ct);
    }

    private async void OnTick(object? state)
    {
        if (_isPolling)
        {
            return;
        }

        _isPolling = true;
        try
        {
            await PollAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StreamStatusProvider poll error");
        }
        finally
        {
            _isPolling = false;
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_channelLogin))
        {
            await LoadChannelLoginAsync(ct);
            if (string.IsNullOrWhiteSpace(_channelLogin))
            {
                return;
            }
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IBroadcasterHelixClient helix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();

        try
        {
            StreamInfo? stream = await helix.GetStreamAsync(_channelLogin!, ct);
            StreamInfo? previous = _currentStream;
            _currentStream = stream;
            Interlocked.Exchange(ref _consecutiveFailures, 0);

            if (stream is not null && previous is null)
            {
                _logger.LogInformation(
                    "Stream went LIVE: {Title} ({Game}) — {Viewers} viewers",
                    stream.Title, stream.GameName, stream.ViewerCount);
            }
            else if (stream is null && previous is not null)
            {
                _logger.LogInformation("Stream went OFFLINE");
            }
        }
        catch (Exception ex)
        {
            int failures = Interlocked.Increment(ref _consecutiveFailures);

            if (failures >= MaxConsecutiveFailuresBeforeOffline && _currentStream is not null)
            {
                _logger.LogWarning(
                    "Stream status poll failed {Failures} consecutive times — resetting to offline (fail-safe)",
                    failures);
                _currentStream = null;
            }
            else
            {
                _logger.LogWarning(
                    ex,
                    "Failed to poll stream status for channel {Channel} (failure {Failures}/{Max})",
                    _channelLogin, failures, MaxConsecutiveFailuresBeforeOffline);
            }
        }
    }

    private async Task LoadChannelLoginAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            _channelLogin = await settings.GetAsync("Bot.Channel", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load channel login for StreamStatusProvider");
        }
    }
}
