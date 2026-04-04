using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Shows the invoking user's total watch time.
/// </summary>
public class WatchtimeCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!watchtime";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public string Description => "Shows your total watch time.";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => "@{user} your watch time is {watchtime}.";

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchtimeCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public WatchtimeCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User user = await users.GetOrCreateAsync(message.UserId, message.Username, ct);

        int hours = user.WatchedMinutes / 60;
        int minutes = user.WatchedMinutes % 60;
        string formatted = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";

        return $"@{message.DisplayName} your watch time is {formatted}.";
    }
}
