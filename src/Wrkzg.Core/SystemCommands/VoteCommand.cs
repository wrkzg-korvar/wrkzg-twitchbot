using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Votes in the active poll. Usage: !vote 1
/// </summary>
public class VoteCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!vote";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!v" };

    /// <inheritdoc />
    public string Description => "Vote in the active poll. Usage: !vote <number>";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoteCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public VoteCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        string args = message.Content.Length > Trigger.Length
            ? message.Content[(Trigger.Length + 1)..].Trim()
            : string.Empty;

        if (string.IsNullOrEmpty(args) || !int.TryParse(args, out int optionNumber))
        {
            return $"@{message.DisplayName}, usage: !vote <number>";
        }

        int optionIndex = optionNumber - 1;

        using IServiceScope scope = _scopeFactory.CreateScope();
        PollService pollService = scope.ServiceProvider.GetRequiredService<PollService>();

        VoteResult result = await pollService.VoteAsync(
            message.UserId, message.Username, optionIndex, ct);

        if (!result.Success)
        {
            return result.Error;
        }

        // Silent confirmation — don't spam the chat
        return null;
    }
}
