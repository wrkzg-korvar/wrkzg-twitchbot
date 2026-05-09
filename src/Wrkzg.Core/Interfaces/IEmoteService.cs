using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>Provides cached Twitch emote data.</summary>
public interface IEmoteService
{
    /// <summary>Gets all currently cached emotes (global + channel).</summary>
    IReadOnlyList<EmoteDto> GetCachedEmotes();

    /// <summary>Forces an immediate refresh of all emotes.</summary>
    Task RefreshAsync(CancellationToken ct = default);
}

/// <summary>Emote data transfer object for API responses.</summary>
public sealed class EmoteDto
{
    /// <summary>The Twitch-assigned emote identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The emote name that users type in chat.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The CDN URL for the emote image.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>The emote source category (e.g. "global", "channel", "subscriber").</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Which account loaded this emote: "bot", "broadcaster", or "shared"
    /// (available to both accounts — global emotes or emotes both accounts subscribe to).
    /// The dashboard EmotePicker filters by this when the streamer toggles "Send as".
    /// </summary>
    public string Owner { get; init; } = "shared";
}
