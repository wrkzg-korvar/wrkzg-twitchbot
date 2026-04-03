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

public class ImportRowError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ImportErrorSeverity Severity { get; set; }
}

public enum ImportErrorSeverity
{
    Warning = 0,
    Error = 1
}
