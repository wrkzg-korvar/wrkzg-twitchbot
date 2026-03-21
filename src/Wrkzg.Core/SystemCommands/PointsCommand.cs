using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Shows the invoking user's current point balance.
/// </summary>
public class PointsCommand : ISystemCommand
{
    public string Trigger => "!points";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Shows your current points.";
    public string? DefaultResponseTemplate => "@{user} you have {points} points.";

    private readonly IServiceScopeFactory _scopeFactory;

    public PointsCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User user = await users.GetOrCreateAsync(message.UserId, message.Username, ct);

        return $"@{message.DisplayName} you have {user.Points.ToString(CultureInfo.InvariantCulture)} points.";
    }
}
