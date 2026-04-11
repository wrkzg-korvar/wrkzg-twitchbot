namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parsed command from a DeepBot chanmsgconfig save file.
/// </summary>
public class ImportCommandRecord
{
    /// <summary>Command trigger including ! prefix.</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>Response template text.</summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>Category/group name (e.g. "Stream", "Soziale Medien").</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Cooldown in seconds.</summary>
    public int CooldownSeconds { get; set; }

    /// <summary>DeepBot access level (0=Everyone, 1=Follower, 2=Sub, 4=Mod, 6=Broadcaster).</summary>
    public int AccessLevel { get; set; }

    /// <summary>Whether the command is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Record number for error reporting.</summary>
    public int RecordNumber { get; set; }
}
