using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Abstraction over the Twitch Helix REST API.
/// Used for stream status polling, user info, and future poll/EventSub management.
/// </summary>
public interface ITwitchHelixClient
{
    /// <summary>
    /// Gets the current stream info for a channel. Returns null if offline.
    /// </summary>
    Task<StreamInfo?> GetStreamAsync(string channelLogin, CancellationToken ct = default);

    /// <summary>
    /// Gets user info by login name.
    /// </summary>
    Task<HelixUserInfo?> GetUserAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Sends a chat message via the Helix API (POST /chat/messages).
    /// Uses the Broadcaster token. Returns true if sent successfully.
    /// </summary>
    Task<bool> SendChatMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default);
}

/// <summary>
/// Stream information from the Helix "Get Streams" endpoint.
/// </summary>
public sealed class StreamInfo
{
    public string Id { get; init; } = string.Empty;
    public string UserLogin { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public int ViewerCount { get; init; }
    public string StartedAt { get; init; } = string.Empty;
}

/// <summary>
/// User information from the Helix "Get Users" endpoint.
/// </summary>
public sealed class HelixUserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
