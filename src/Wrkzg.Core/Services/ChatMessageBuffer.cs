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

    public void Add(ChatMessage message)
    {
        _messages.Enqueue(message);
        while (_messages.Count > MaxMessages)
        {
            _messages.TryDequeue(out _);
        }
    }

    public IReadOnlyList<ChatMessage> GetRecent()
    {
        return _messages.ToArray();
    }
}
