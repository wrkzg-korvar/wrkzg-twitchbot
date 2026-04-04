using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Ends the active poll. Moderator+ only.
/// </summary>
public class PollEndCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!pollend";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!endpoll", "!closepoll" };

    /// <inheritdoc />
    public string Description => "End the active poll. Mod only.";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollEndCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public PollEndCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods can end polls.";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        PollService pollService = scope.ServiceProvider.GetRequiredService<PollService>();

        PollResult result = await pollService.EndPollAsync(PollEndReason.ManuallyClosed, ct);
        if (!result.Success)
        {
            return $"@{message.DisplayName}, {result.Error}";
        }

        // PollService already announces results in chat
        return null;
    }
}
