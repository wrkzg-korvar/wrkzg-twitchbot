using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Manages background import jobs. One job at a time (SQLite single-writer).
/// Jobs survive frontend navigation — they run in the server process.
/// </summary>
public class ImportJobService : IImportJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ILogger<ImportJobService> _logger;

    private readonly ConcurrentDictionary<string, ImportJob> _jobs = new();
    private readonly SemaphoreSlim _importLock = new(1, 1);
    private CancellationTokenSource? _activeJobCts;

    private static readonly Dictionary<ImportSourceType, string[]> ModuleLockMap = new()
    {
        [ImportSourceType.DeepbotCsv] = ["/users"],
        [ImportSourceType.DeepbotJson] = ["/users"],
        [ImportSourceType.DeepbotBin] = ["/users"],
        [ImportSourceType.GenericCsv] = ["/users"],
        [ImportSourceType.StreamlabsChatbot] = ["/users"],
        [ImportSourceType.DeepbotBinConfig] = ["/commands", "/quotes", "/timers"],
    };

    public ImportJobService(
        IServiceScopeFactory scopeFactory,
        IChatEventBroadcaster broadcaster,
        ILogger<ImportJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task<ImportJob> StartAsync(Stream fileStream, ImportConfiguration config, CancellationToken ct = default)
    {
        if (!await _importLock.WaitAsync(TimeSpan.Zero, ct))
        {
            throw new InvalidOperationException("Another import is already running.");
        }

        ImportJob job = new()
        {
            SourceType = config.SourceType,
            Status = ImportJobStatus.Queued,
            LockedModules = ModuleLockMap.GetValueOrDefault(config.SourceType, []),
        };

        _jobs[job.Id] = job;

        // Save file to temp directory
        string tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wrkzg", "import-temp");
        Directory.CreateDirectory(tempDir);

        string tempFile = Path.Combine(tempDir, $"{job.Id}.bin");
        using (FileStream fs = File.Create(tempFile))
        {
            await fileStream.CopyToAsync(fs, ct);
        }

        _activeJobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CancellationToken jobCt = _activeJobCts.Token;

        // Fire and forget — the job runs in the background
        _ = Task.Run(() => ExecuteJobAsync(job, tempFile, config, jobCt), jobCt);

        _logger.LogInformation("Import job {JobId} queued for {SourceType}", job.Id, config.SourceType);
        return job;
    }

    private async Task ExecuteJobAsync(ImportJob job, string tempFile, ImportConfiguration config, CancellationToken ct)
    {
        try
        {
            job.Status = ImportJobStatus.Importing;
            await BroadcastProgressAsync(job, ct);

            using IServiceScope scope = _scopeFactory.CreateScope();
            IDataImportService importService = scope.ServiceProvider.GetRequiredService<IDataImportService>();

            // Progress callback that updates the job and broadcasts to clients
            Progress<int> progress = new(async percent =>
            {
                job.ProgressPercent = percent;
                try
                {
                    await BroadcastProgressAsync(job, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to broadcast import progress");
                }
            });

            using FileStream fs = File.OpenRead(tempFile);
            ImportResult result = await importService.ExecuteAsync(fs, config, progress, ct);

            job.Status = ImportJobStatus.Complete;
            job.Result = result;
            job.CompletedAt = DateTimeOffset.UtcNow;
            job.ProgressPercent = 100;
            job.ProcessedRecords = result.TotalRows;
            job.TotalRecords = result.TotalRows;

            await BroadcastCompleteAsync(job, ct);
            _logger.LogInformation("Import job {JobId} completed: {Summary}", job.Id, result.Summary);
        }
        catch (OperationCanceledException)
        {
            job.Status = ImportJobStatus.Cancelled;
            job.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("Import job {JobId} cancelled", job.Id);
        }
        catch (Exception ex)
        {
            job.Status = ImportJobStatus.Error;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await BroadcastErrorAsync(job, CancellationToken.None);
            _logger.LogError(ex, "Import job {JobId} failed", job.Id);
        }
        finally
        {
            _importLock.Release();
            _activeJobCts = null;

            // Clean up temp file
            try { File.Delete(tempFile); }
            catch { /* ignore */ }
        }
    }

    public IReadOnlyList<ImportJob> GetAll() => _jobs.Values.OrderByDescending(j => j.StartedAt).ToList();

    public ImportJob? GetById(string jobId) => _jobs.GetValueOrDefault(jobId);

    public bool Cancel(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out ImportJob? job) && job.Status <= ImportJobStatus.Importing)
        {
            _activeJobCts?.Cancel();
            return true;
        }
        return false;
    }

    public string[] GetLockedModules()
    {
        return _jobs.Values
            .Where(j => j.Status is ImportJobStatus.Queued or ImportJobStatus.Analyzing or ImportJobStatus.Importing)
            .SelectMany(j => j.LockedModules)
            .Distinct()
            .ToArray();
    }

    // --- SignalR Broadcasts ---

    private Task BroadcastProgressAsync(ImportJob job, CancellationToken ct)
    {
        return _broadcaster.BroadcastImportProgressAsync(new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            processedRecords = job.ProcessedRecords,
            totalRecords = job.TotalRecords,
            progressPercent = job.ProgressPercent,
            lockedModules = job.LockedModules,
        }, ct);
    }

    private Task BroadcastCompleteAsync(ImportJob job, CancellationToken ct)
    {
        return _broadcaster.BroadcastImportCompleteAsync(new
        {
            jobId = job.Id,
            result = job.Result,
        }, ct);
    }

    private Task BroadcastErrorAsync(ImportJob job, CancellationToken ct)
    {
        return _broadcaster.BroadcastImportErrorAsync(new
        {
            jobId = job.Id,
            errorMessage = job.ErrorMessage,
        }, ct);
    }
}
