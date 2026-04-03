using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Interface for all chat games. Games are Singleton services
/// that manage their own state (active rounds, participants, timers).
/// </summary>
public interface IChatGame
{
    /// <summary>The primary command trigger (e.g. "!heist").</summary>
    string Trigger { get; }

    /// <summary>Alternative triggers.</summary>
    string[] Aliases { get; }

    /// <summary>Human-readable name for the dashboard.</summary>
    string Name { get; }

    /// <summary>Human-readable description for the dashboard.</summary>
    string Description { get; }

    /// <summary>Whether this game is currently enabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Minimum role priority required to participate. 0 = Everyone.</summary>
    int MinRolePriority { get; set; }

    /// <summary>
    /// Handles a chat message that matches this game's trigger.
    /// Returns a chat response or null if handled silently.
    /// </summary>
    Task<string?> HandleAsync(ChatMessage message, CancellationToken ct = default);

    /// <summary>
    /// Called when a message is received during an active game round
    /// (e.g. trivia answers, duel !accept). Returns true if the message was consumed.
    /// </summary>
    Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default);

    /// <summary>Returns the current message templates (custom or default).</summary>
    Dictionary<string, string> GetMessageTemplates();

    /// <summary>Returns the default message templates (for reset).</summary>
    Dictionary<string, string> GetDefaultMessageTemplates();
}
