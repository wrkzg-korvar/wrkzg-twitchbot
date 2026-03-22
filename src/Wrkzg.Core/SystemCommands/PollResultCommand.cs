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
/// Shows current poll results in chat.
/// </summary>
public class PollResultCommand : ISystemCommand
{
    public string Trigger => "!pollresult";
    public string[] Aliases => new[] { "!pollresults", "!results" };
    public string Description => "Show current poll results.";
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    public PollResultCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        PollService pollService = scope.ServiceProvider.GetRequiredService<PollService>();

        PollResultsDto? results = await pollService.GetResultsAsync(ct: ct);
        if (results is null)
        {
            return $"@{message.DisplayName}, no active poll.";
        }

        string opts = string.Join(" | ",
            results.Options.Select(o => $"{o.Label}: {o.Votes} ({o.Percentage}%)"));

        string status = results.IsActive ? "\ud83d\udd34 LIVE" : "\u2705 Ended";
        return $"{status} {results.Question} \u2014 {opts} ({results.TotalVotes} votes)";
    }
}
