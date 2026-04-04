namespace Wrkzg.Core.Models;

/// <summary>
/// Bot IRC connection status, pushed to the frontend via SignalR.
/// </summary>
public class BotStatus
{
    /// <summary>Whether the bot is currently connected to Twitch IRC.</summary>
    public bool IsConnected { get; init; }

    /// <summary>The Twitch channel the bot is connected to, or null if disconnected.</summary>
    public string? Channel { get; init; }

    /// <summary>Human-readable reason for the current state (e.g. disconnect cause).</summary>
    public string? Reason { get; init; }
}
