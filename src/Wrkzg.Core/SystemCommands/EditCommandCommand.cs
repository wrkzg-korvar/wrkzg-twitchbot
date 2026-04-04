using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Allows Moderators and the Broadcaster to edit custom command responses from chat.
/// Usage: !editcmd !trigger New response text here
/// </summary>
public class EditCommandCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!editcmd";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!editcommand" };

    /// <inheritdoc />
    public string Description => "Edit a custom command's response. Usage: !editcmd !trigger New response";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditCommandCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public EditCommandCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        // Permission check: Moderator+ only
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only moderators can edit commands.";
        }

        // Parse: !editcmd !trigger New response text
        string[] parts = message.Content.Split(' ', 3);
        if (parts.Length < 3)
        {
            return $"@{message.DisplayName}, usage: !editcmd !trigger New response text";
        }

        string targetTrigger = parts[1].ToLowerInvariant();
        string newResponse = parts[2];

        if (!targetTrigger.StartsWith('!'))
        {
            return $"@{message.DisplayName}, trigger must start with '!' — usage: !editcmd !trigger New response";
        }

        if (newResponse.Length > 500)
        {
            return $"@{message.DisplayName}, response must be 500 characters or less.";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        ICommandRepository commands = scope.ServiceProvider.GetRequiredService<ICommandRepository>();

        Command? command = await commands.GetByTriggerOrAliasAsync(targetTrigger, ct);
        if (command is null)
        {
            return $"@{message.DisplayName}, command {targetTrigger} not found.";
        }

        command.ResponseTemplate = newResponse;
        await commands.UpdateAsync(command, ct);

        return $"@{message.DisplayName}, updated {command.Trigger} → {newResponse}";
    }
}
