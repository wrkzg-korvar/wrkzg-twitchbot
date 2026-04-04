using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Services;

/// <summary>
/// In-memory ring buffer holding the most recent chat messages.
/// Used to populate the dashboard chat feed on initial page load.
/// </summary>
public class ChatMessageBuffer
{
    private readonly ConcurrentQueue<ChatMessage> _messages = new();
    private const int MaxMessages = 15;

    /// <summary>
    /// Adds a chat message to the buffer, evicting the oldest message if the buffer is full.
    /// </summary>
    /// <param name="message">The chat message to add.</param>
    public void Add(ChatMessage message)
    {
        _messages.Enqueue(message);
        while (_messages.Count > MaxMessages)
        {
            _messages.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Returns the most recent chat messages, optionally filtered by Twitch user ID.
    /// </summary>
    /// <param name="count">Maximum number of messages to return. Defaults to 15.</param>
    /// <param name="twitchUserId">Optional Twitch user ID to filter messages by a specific user.</param>
    /// <returns>A read-only list of the most recent matching chat messages.</returns>
    public IReadOnlyList<ChatMessage> GetRecent(int count = 15, string? twitchUserId = null)
    {
        if (twitchUserId is null)
        {
            return _messages.ToArray().TakeLast(count).ToArray();
        }

        return _messages
            .Where(m => m.UserId == twitchUserId)
            .TakeLast(count)
            .ToArray();
    }
}
