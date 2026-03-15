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
    public string Trigger => "!commands";
    public string[] Aliases => new[] { "!help" };
    public string Description => "Lists all available commands.";

    private readonly IServiceScopeFactory _scopeFactory;

    public CommandsListCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ICommandRepository repo = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
        IReadOnlyList<Command> commands = await repo.GetAllAsync(ct);

        IEnumerable<string> enabled = commands.Where(c => c.IsEnabled).Select(c => c.Trigger);
        string[] systemCmds = new[] { "!commands", "!points", "!watchtime", "!followage" };
        IEnumerable<string> all = systemCmds.Concat(enabled);

        return $"Available commands: {string.Join(", ", all)}";
    }
}
