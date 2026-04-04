namespace Wrkzg.Core.Models;

/// <summary>
/// Configuration for the chat spam filter. Stored as individual Settings keys.
/// </summary>
public class SpamFilterConfig
{
    // ─── Link Filter ─────────────────────────────

    /// <summary>Whether the link filter is active.</summary>
    public bool LinksEnabled { get; set; } = true;

    /// <summary>Timeout duration in seconds when a non-whitelisted link is posted.</summary>
    public int LinksTimeoutSeconds { get; set; } = 10;

    /// <summary>Whether subscribers are exempt from the link filter.</summary>
    public bool LinksSubsExempt { get; set; } = true;

    /// <summary>Whether moderators are exempt from the link filter.</summary>
    public bool LinksModsExempt { get; set; } = true;

    /// <summary>Comma-separated list of allowed domains that bypass the link filter.</summary>
    public string LinkWhitelist { get; set; } = "clips.twitch.tv,twitch.tv,youtube.com,youtu.be";

    // ─── Caps Filter ─────────────────────────────

    /// <summary>Whether the excessive caps filter is active.</summary>
    public bool CapsEnabled { get; set; } = true;

    /// <summary>Minimum message length before the caps filter applies.</summary>
    public int CapsMinLength { get; set; } = 10;

    /// <summary>Maximum allowed percentage of uppercase characters (0-100).</summary>
    public int CapsMaxPercent { get; set; } = 70;

    /// <summary>Timeout duration in seconds for excessive caps violations.</summary>
    public int CapsTimeoutSeconds { get; set; } = 5;

    /// <summary>Whether subscribers are exempt from the caps filter.</summary>
    public bool CapsSubsExempt { get; set; } = true;

    // ─── Banned Words ────────────────────────────

    /// <summary>Whether the banned words filter is active.</summary>
    public bool BannedWordsEnabled { get; set; } = true;

    /// <summary>Comma-separated list of banned words and phrases.</summary>
    public string BannedWordsList { get; set; } = string.Empty;

    /// <summary>Timeout duration in seconds for banned word violations.</summary>
    public int BannedWordsTimeoutSeconds { get; set; } = 300;

    /// <summary>Whether subscribers are exempt from the banned words filter.</summary>
    public bool BannedWordsSubsExempt { get; set; }

    // ─── Emote Spam ──────────────────────────────

    /// <summary>Whether the emote spam filter is active.</summary>
    public bool EmoteSpamEnabled { get; set; }

    /// <summary>Maximum number of emotes allowed in a single message.</summary>
    public int EmoteSpamMaxEmotes { get; set; } = 15;

    /// <summary>Timeout duration in seconds for emote spam violations.</summary>
    public int EmoteSpamTimeoutSeconds { get; set; } = 5;

    /// <summary>Whether subscribers are exempt from the emote spam filter.</summary>
    public bool EmoteSpamSubsExempt { get; set; } = true;

    // ─── Repetition Filter ───────────────────────

    /// <summary>Whether the repeated message filter is active.</summary>
    public bool RepeatEnabled { get; set; }

    /// <summary>Maximum number of identical consecutive messages before triggering.</summary>
    public int RepeatMaxCount { get; set; } = 3;

    /// <summary>Timeout duration in seconds for repetition violations.</summary>
    public int RepeatTimeoutSeconds { get; set; } = 10;

    /// <summary>Whether subscribers are exempt from the repetition filter.</summary>
    public bool RepeatSubsExempt { get; set; } = true;
}
