using System.Collections.Generic;
using System.Linq;

namespace Wrkzg.Core.Models;

/// <summary>
/// Result of a bot data import operation.
/// </summary>
public class ImportResult
{
    /// <summary>Total rows/entries in the source file.</summary>
    public int TotalRows { get; set; }

    /// <summary>Successfully imported/updated users.</summary>
    public int ImportedCount { get; set; }

    /// <summary>Rows that were skipped (duplicates, errors).</summary>
    public int SkippedCount { get; set; }

    /// <summary>New users created (not previously in Wrkzg DB).</summary>
    public int CreatedCount { get; set; }

    /// <summary>Existing users that were updated/merged.</summary>
    public int UpdatedCount { get; set; }

    /// <summary>Roles assigned from VIP level mapping.</summary>
    public int RolesAssignedCount { get; set; }

    /// <summary>Per-row errors/warnings.</summary>
    public List<ImportRowError> Errors { get; set; } = new();

    /// <summary>Whether the import completed successfully.</summary>
    public bool Success => !Errors.Any(e => e.Severity == ImportErrorSeverity.Error);

    /// <summary>Summary message for display.</summary>
    public string Summary => $"Imported {ImportedCount}/{TotalRows} users ({CreatedCount} new, {UpdatedCount} updated, {SkippedCount} skipped)";
}

/// <summary>
/// Describes an error or warning that occurred while importing a single row.
/// </summary>
public class ImportRowError
{
    /// <summary>One-based row number in the source file where the error occurred.</summary>
    public int RowNumber { get; set; }

    /// <summary>Name of the field that caused the error (e.g. "username", "points").</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Human-readable description of the error.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether this is a recoverable warning or a fatal row error.</summary>
    public ImportErrorSeverity Severity { get; set; }
}

/// <summary>
/// Severity level for import row errors.
/// </summary>
public enum ImportErrorSeverity
{
    /// <summary>Non-fatal issue; the row was still processed.</summary>
    Warning = 0,

    /// <summary>Fatal issue; the row was skipped.</summary>
    Error = 1
}
