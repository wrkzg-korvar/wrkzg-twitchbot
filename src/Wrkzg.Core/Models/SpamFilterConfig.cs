namespace Wrkzg.Core.Models;

/// <summary>
/// Configuration for the chat spam filter. Stored as individual Settings keys.
/// </summary>
public class SpamFilterConfig
{
    // ─── Link Filter ─────────────────────────────
    public bool LinksEnabled { get; set; } = true;
    public int LinksTimeoutSeconds { get; set; } = 10;
    public bool LinksSubsExempt { get; set; } = true;
    public bool LinksModsExempt { get; set; } = true;
    public string LinkWhitelist { get; set; } = "clips.twitch.tv,twitch.tv,youtube.com,youtu.be";

    // ─── Caps Filter ─────────────────────────────
    public bool CapsEnabled { get; set; } = true;
    public int CapsMinLength { get; set; } = 10;
    public int CapsMaxPercent { get; set; } = 70;
    public int CapsTimeoutSeconds { get; set; } = 5;
    public bool CapsSubsExempt { get; set; } = true;

    // ─── Banned Words ────────────────────────────
    public bool BannedWordsEnabled { get; set; } = true;
    public string BannedWordsList { get; set; } = string.Empty;
    public int BannedWordsTimeoutSeconds { get; set; } = 300;
    public bool BannedWordsSubsExempt { get; set; }

    // ─── Emote Spam ──────────────────────────────
    public bool EmoteSpamEnabled { get; set; }
    public int EmoteSpamMaxEmotes { get; set; } = 15;
    public int EmoteSpamTimeoutSeconds { get; set; } = 5;
    public bool EmoteSpamSubsExempt { get; set; } = true;

    // ─── Repetition Filter ───────────────────────
    public bool RepeatEnabled { get; set; }
    public int RepeatMaxCount { get; set; } = 3;
    public int RepeatTimeoutSeconds { get; set; } = 10;
    public bool RepeatSubsExempt { get; set; } = true;
}
