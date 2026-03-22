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
    public string Trigger => "!vote";
    public string[] Aliases => new[] { "!v" };
    public string Description => "Vote in the active poll. Usage: !vote <number>";
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    public VoteCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

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
