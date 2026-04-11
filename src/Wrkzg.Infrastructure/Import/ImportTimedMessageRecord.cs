namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parsed timed message from a DeepBot chanmsgconfig save file.
/// </summary>
public class ImportTimedMessageRecord
{
    /// <summary>Timer display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Message text to send in chat.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether the timer is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether the message should be sent as /announce.</summary>
    public bool IsAnnouncement { get; set; }

    /// <summary>Record number for error reporting.</summary>
    public int RecordNumber { get; set; }
}
