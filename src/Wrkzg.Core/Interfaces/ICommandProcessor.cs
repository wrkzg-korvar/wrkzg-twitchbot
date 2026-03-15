using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Processes incoming chat messages, checks for command triggers,
/// validates permissions and cooldowns, resolves response templates,
/// and sends the response back to chat.
///
/// This is the central orchestrator for the command pipeline:
///   ChatMessage → trigger match → permission check → cooldown check
///   → template resolution → send response → update stats
/// </summary>
public interface ICommandProcessor
{
    /// <summary>
    /// Processes a chat message. If it matches a command trigger,
    /// executes the command and sends the response to chat.
    /// Returns true if a command was executed, false otherwise.
    /// </summary>
    Task<bool> HandleMessageAsync(ChatMessage message, CancellationToken ct = default);
}
