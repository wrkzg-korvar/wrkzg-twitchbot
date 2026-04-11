using System;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parsed user record from an import file.
/// Used by all parsers (Deepbot CSV, Deepbot JSON, Generic CSV).
/// </summary>
public class ImportUserRecord
{
    /// <summary>Gets or sets the Twitch username (lowercased).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's loyalty points.</summary>
    public long Points { get; set; }

    /// <summary>Gets or sets the total watched time in minutes.</summary>
    public int WatchedMinutes { get; set; }

    /// <summary>Gets or sets the normalized VIP level (1=Bronze, 2=Silver, 3=Gold). Only available in Deepbot JSON imports.</summary>
    public int? VipLevel { get; set; }

    /// <summary>Gets or sets the moderator level. Only available in Deepbot JSON imports.</summary>
    public int? ModLevel { get; set; }

    /// <summary>Gets or sets the date the user first joined the channel. Only available in Deepbot JSON imports.</summary>
    public DateTimeOffset? JoinDate { get; set; }

    /// <summary>Gets or sets the date the user was last seen in chat. Only available in Deepbot JSON imports.</summary>
    public DateTimeOffset? LastSeen { get; set; }

    /// <summary>Gets or sets the Twitch user ID. Available in DeepBot binary imports.</summary>
    public string? TwitchId { get; set; }

    /// <summary>Gets or sets the display name with correct capitalization.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the source file line number for error reporting.</summary>
    public int LineNumber { get; set; }
}
