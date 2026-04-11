using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// Configuration for a data import operation.
/// </summary>
public class ImportConfiguration
{
    /// <summary>Source bot type.</summary>
    public ImportSourceType SourceType { get; set; }

    /// <summary>
    /// How to handle users that already exist in Wrkzg.
    /// </summary>
    public ImportConflictStrategy ConflictStrategy { get; set; } = ImportConflictStrategy.KeepHigher;

    /// <summary>
    /// Whether to map Deepbot VIP levels to Wrkzg Roles.
    /// Only applicable for Deepbot JSON imports.
    /// </summary>
    public bool MapVipToRoles { get; set; } = true;

    /// <summary>
    /// VIP level to Role ID mapping.
    /// Key: Deepbot VIP level (1=Bronze, 2=Silver, 3=Gold)
    /// Value: Wrkzg Role ID to assign
    /// </summary>
    public Dictionary<int, int>? VipRoleMapping { get; set; }

    /// <summary>
    /// For generic CSV: custom column mapping.
    /// Key: Wrkzg field name (username, points, watchedMinutes)
    /// Value: Column index (0-based) or header name
    /// </summary>
    public Dictionary<string, string>? ColumnMapping { get; set; }

    /// <summary>Whether the CSV file has a header row.</summary>
    public bool HasHeader { get; set; }

    /// <summary>CSV delimiter character.</summary>
    public char Delimiter { get; set; } = ',';
}

/// <summary>
/// Identifies the source bot format for data import.
/// </summary>
public enum ImportSourceType
{
    /// <summary>Deepbot CSV export file.</summary>
    DeepbotCsv = 0,

    /// <summary>Deepbot JSON export file (includes VIP levels).</summary>
    DeepbotJson = 1,

    /// <summary>Streamlabs Chatbot export file.</summary>
    StreamlabsChatbot = 2,

    /// <summary>Generic CSV with user-defined column mapping.</summary>
    GenericCsv = 3,

    /// <summary>DeepBot binary save file (users*.bin — gzip-compressed protobuf).</summary>
    DeepbotBin = 4,

    /// <summary>DeepBot config save file (chanmsgconfig*.bin — commands, quotes, timed messages).</summary>
    DeepbotBinConfig = 5
}

/// <summary>Strategy for handling conflicts when a user already exists during import.</summary>
public enum ImportConflictStrategy
{
    /// <summary>Skip users that already exist.</summary>
    Skip = 0,

    /// <summary>Overwrite existing users with imported data.</summary>
    Overwrite = 1,

    /// <summary>Keep the higher value for Points and WatchedMinutes (merge).</summary>
    KeepHigher = 2,

    /// <summary>Add imported Points/Minutes to existing values.</summary>
    Add = 3
}
