using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Cancels the active raffle. Moderator+ only.
/// </summary>
public class CancelRaffleCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!cancelraffle";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!rafflecancel" };

    /// <inheritdoc />
    public string Description => "Cancel the active raffle. Mod only.";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelRaffleCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public CancelRaffleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods can cancel raffles.";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        RaffleService raffleService = scope.ServiceProvider.GetRequiredService<RaffleService>();

        RaffleResult result = await raffleService.CancelAsync(ct);
        if (!result.Success)
        {
            return $"@{message.DisplayName}, {result.Error}";
        }

        return null;
    }
}
