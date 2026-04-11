using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Background service that checks every 2 seconds if the active poll has expired.
/// </summary>
public class PollTimerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PollTimerService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PollTimerService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve the scoped <see cref="PollService"/>.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public PollTimerService(IServiceScopeFactory scopeFactory, ILogger<PollTimerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PollTimerService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool hasActive = false;

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                PollService pollService = scope.ServiceProvider.GetRequiredService<PollService>();
                hasActive = await pollService.CheckExpiredPollsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error checking expired polls");
            }

            // Adaptive polling: 2s when active, 15s when idle
            TimeSpan delay = hasActive ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(15);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
