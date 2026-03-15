using System;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Abstraction over the Twitch IRC chat connection.
/// Implemented in Infrastructure via TwitchLib.Client.
/// Registered as Singleton — one IRC connection per app instance.
/// </summary>
public interface ITwitchChatClient : IAsyncDisposable
{
    /// <summary>Fired for every chat message received in the joined channel.</summary>
    event Func<ChatMessage, Task>? OnMessageReceived;

    /// <summary>Fired when a user joins the channel.</summary>
    event Func<string, Task>? OnUserJoined;

    /// <summary>Fired when the bot successfully connects and joins the channel.</summary>
    event Func<Task>? OnConnected;

    /// <summary>Fired when the bot disconnects (intentionally or due to error).</summary>
    event Func<Task>? OnDisconnected;

    /// <summary>Whether the client is currently connected to IRC.</summary>
    bool IsConnected { get; }

    /// <summary>The channel the bot is currently joined to (lowercase, without #). Null if not connected.</summary>
    string? JoinedChannel { get; }

    /// <summary>Connects to Twitch IRC using the stored Bot token and joins the specified channel.</summary>
    Task ConnectAsync(string channel, CancellationToken ct = default);

    /// <summary>Disconnects from Twitch IRC gracefully.</summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>Sends a chat message to the currently joined channel.</summary>
    Task SendMessageAsync(string message, CancellationToken ct = default);

    /// <summary>Sends a reply to a specific message (threaded reply).</summary>
    Task SendReplyAsync(string replyToMessageId, string message, CancellationToken ct = default);
}
