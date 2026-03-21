using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Shows how long the invoking user has been following the channel.
/// </summary>
public class FollowageCommand : ISystemCommand
{
    public string Trigger => "!followage";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Shows how long you've been following.";
    public string? DefaultResponseTemplate => "@{user} you've been following for {followage}.";

    private readonly IServiceScopeFactory _scopeFactory;

    public FollowageCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User user = await users.GetOrCreateAsync(message.UserId, message.Username, ct);

        if (user.FollowDate is null)
        {
            return $"@{message.DisplayName} you are not following the channel.";
        }

        TimeSpan duration = DateTimeOffset.UtcNow - user.FollowDate.Value;
        string formatted;

        if (duration.TotalDays >= 365)
        {
            int years = (int)(duration.TotalDays / 365);
            int months = (int)((duration.TotalDays % 365) / 30);
            formatted = months > 0 ? $"{years}y {months}mo" : $"{years}y";
        }
        else if (duration.TotalDays >= 30)
        {
            int months = (int)(duration.TotalDays / 30);
            int days = (int)(duration.TotalDays % 30);
            formatted = days > 0 ? $"{months}mo {days}d" : $"{months}mo";
        }
        else
        {
            formatted = $"{(int)duration.TotalDays}d";
        }

        return $"@{message.DisplayName} you've been following for {formatted}.";
    }
}
