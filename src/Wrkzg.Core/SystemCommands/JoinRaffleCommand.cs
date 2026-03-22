using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Enters the active raffle. Usage: !join
/// </summary>
public class JoinRaffleCommand : ISystemCommand
{
    public string Trigger => "!join";
    public string[] Aliases => new[] { "!enter" };
    public string Description => "Enter the active raffle.";
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    public JoinRaffleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        RaffleService raffleService = scope.ServiceProvider.GetRequiredService<RaffleService>();

        EntryResult result = await raffleService.EnterAsync(
            message.UserId, message.Username, ct);

        if (!result.Success)
        {
            return result.Error?.Replace("{user}", message.DisplayName);
        }

        return null;
    }
}
