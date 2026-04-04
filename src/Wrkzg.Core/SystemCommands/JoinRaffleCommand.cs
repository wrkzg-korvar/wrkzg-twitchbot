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
    /// <inheritdoc />
    public string Trigger => "!join";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!enter" };

    /// <inheritdoc />
    public string Description => "Enter the active raffle.";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinRaffleCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public JoinRaffleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
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
