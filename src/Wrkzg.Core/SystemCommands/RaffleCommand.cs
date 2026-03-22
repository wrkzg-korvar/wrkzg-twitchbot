using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Starts a new raffle. Moderator+ only.
/// Usage: !raffle Title [| keyword=word] [| duration=sec] [| max=n]
/// </summary>
public class RaffleCommand : ISystemCommand
{
    public string Trigger => "!raffle";
    public string[] Aliases => new[] { "!giveaway" };
    public string Description => "Start a raffle. Usage: !raffle <title> [| keyword=<word>] [| duration=<sec>] [| max=<n>]";
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    public RaffleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods can start raffles.";
        }

        string args = message.Content.Length > Trigger.Length
            ? message.Content[(Trigger.Length + 1)..].Trim()
            : string.Empty;

        if (string.IsNullOrEmpty(args))
        {
            return $"@{message.DisplayName}, usage: !raffle <title> [| keyword=<word>] [| duration=<sec>] [| max=<n>]";
        }

        string[] parts = args.Split('|', StringSplitOptions.TrimEntries);
        string title = parts[0];
        string? keyword = null;
        int? duration = null;
        int? maxEntries = null;

        foreach (string part in parts.Skip(1))
        {
            string[] kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
            {
                continue;
            }

            switch (kv[0].ToLowerInvariant())
            {
                case "keyword":
                    keyword = kv[1];
                    break;
                case "duration":
                    if (int.TryParse(kv[1], out int d))
                    {
                        duration = d;
                    }
                    break;
                case "max":
                    if (int.TryParse(kv[1], out int m))
                    {
                        maxEntries = m;
                    }
                    break;
            }
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        RaffleService raffleService = scope.ServiceProvider.GetRequiredService<RaffleService>();

        RaffleResult result = await raffleService.CreateAsync(
            title, keyword, duration, maxEntries, message.DisplayName, ct);

        if (!result.Success)
        {
            return $"@{message.DisplayName}, {result.Error}";
        }

        return null;
    }
}
