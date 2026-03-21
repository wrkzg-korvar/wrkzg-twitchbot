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
    public string Trigger => "!watchtime";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Shows your total watch time.";
    public string? DefaultResponseTemplate => "@{user} your watch time is {watchtime}.";

    private readonly IServiceScopeFactory _scopeFactory;

    public WatchtimeCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

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
