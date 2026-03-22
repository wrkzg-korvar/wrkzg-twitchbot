using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Draws a winner from the active raffle. Moderator+ only.
/// </summary>
public class DrawRaffleCommand : ISystemCommand
{
    public string Trigger => "!draw";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Draw the raffle winner. Mod only.";
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    public DrawRaffleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods can draw the raffle.";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        RaffleService raffleService = scope.ServiceProvider.GetRequiredService<RaffleService>();

        DrawResult result = await raffleService.DrawAsync(ct);
        if (!result.Success)
        {
            return $"@{message.DisplayName}, {result.Error}";
        }

        return null;
    }
}
