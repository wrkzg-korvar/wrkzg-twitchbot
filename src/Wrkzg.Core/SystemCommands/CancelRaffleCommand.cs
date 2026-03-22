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
    public string Trigger => "!cancelraffle";
    public string[] Aliases => new[] { "!rafflecancel" };
    public string Description => "Cancel the active raffle. Mod only.";
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    public CancelRaffleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

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
