using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// A built-in system command that is always available and cannot be deleted.
/// Checked before custom (DB) commands in the processing pipeline.
/// </summary>
public interface ISystemCommand
{
    /// <summary>Primary trigger (e.g. "!commands").</summary>
    string Trigger { get; }

    /// <summary>Alternative triggers (e.g. "!help" for the commands list).</summary>
    string[] Aliases { get; }

    /// <summary>Short description shown in the dashboard and in !commands output.</summary>
    string Description { get; }

    /// <summary>
    /// Default response template. Shown in the dashboard for reference.
    /// Null for commands that generate dynamic responses (e.g. !commands).
    /// </summary>
    string? DefaultResponseTemplate { get; }

    /// <summary>
    /// Handles the command. Returns the response string to send to chat,
    /// or null if the command should not respond.
    /// </summary>
    Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default);
}
