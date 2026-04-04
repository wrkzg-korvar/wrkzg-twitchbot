using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Lists all available commands (system + custom) in chat.
/// </summary>
public class CommandsListCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!commands";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!help" };

    /// <inheritdoc />
    public string Description => "Lists all available commands.";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => "Available commands: {commandlist}";

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsListCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public CommandsListCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ICommandRepository repo = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
        IReadOnlyList<Command> commands = await repo.GetAllAsync(ct);

        IEnumerable<string> enabled = commands.Where(c => c.IsEnabled).Select(c => c.Trigger);
        string[] systemCmds = new[] { "!commands", "!points", "!watchtime", "!followage", "!editcmd" };
        string commandList = string.Join(", ", systemCmds.Concat(enabled));

        // Use DefaultResponseTemplate with {commandlist} replaced
        string template = DefaultResponseTemplate!;
        return template
            .Replace("{commandlist}", commandList, StringComparison.OrdinalIgnoreCase)
            .Replace("{user}", message.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
}
