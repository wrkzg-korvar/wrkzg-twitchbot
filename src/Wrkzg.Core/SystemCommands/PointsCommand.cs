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
    /// <inheritdoc />
    public string Trigger => "!points";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public string Description => "Shows your current points.";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => "@{user} you have {points} points.";

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PointsCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public PointsCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User user = await users.GetOrCreateAsync(message.UserId, message.Username, ct);

        return $"@{message.DisplayName} you have {user.Points.ToString(CultureInfo.InvariantCulture)} points.";
    }
}
