using System;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parsed user record from an import file.
/// Used by all parsers (Deepbot CSV, Deepbot JSON, Generic CSV).
/// </summary>
public class ImportUserRecord
{
    public string Username { get; set; } = string.Empty;
    public long Points { get; set; }
    public int WatchedMinutes { get; set; }

    // Extended fields (only available in Deepbot JSON imports)
    public int? VipLevel { get; set; }
    public int? ModLevel { get; set; }
    public DateTimeOffset? JoinDate { get; set; }
    public DateTimeOffset? LastSeen { get; set; }

    // Tracking
    public int LineNumber { get; set; }
}
