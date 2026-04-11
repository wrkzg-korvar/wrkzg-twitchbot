using System;

namespace Wrkzg.Core.Models;

/// <summary>Represents a background import job.</summary>
public class ImportJob
{
    /// <summary>Unique job identifier (GUID).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Source format being imported.</summary>
    public ImportSourceType SourceType { get; set; }

    /// <summary>Current job status.</summary>
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Queued;

    /// <summary>Total records to process.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Records processed so far.</summary>
    public int ProcessedRecords { get; set; }

    /// <summary>Completion percentage (0.0 - 100.0).</summary>
    public float ProgressPercent { get; set; }

    /// <summary>Final result (null until complete).</summary>
    public ImportResult? Result { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>When the job was queued.</summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the job completed (or failed).</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Frontend routes that should be locked during this import.</summary>
    public string[] LockedModules { get; set; } = [];
}

/// <summary>Import job lifecycle status.</summary>
public enum ImportJobStatus
{
    Queued = 0,
    Analyzing = 1,
    Importing = 2,
    Complete = 3,
    Error = 4,
    Cancelled = 5
}
