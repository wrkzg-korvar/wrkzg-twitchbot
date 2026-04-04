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
/// Starts a new bot-native poll. Moderator+ only.
/// Usage: !poll 60 Question | Option1 | Option2 [| ...]
/// </summary>
public class PollCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!poll";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public string Description => "Start a poll. Usage: !poll <seconds> <Question> | <Option1> | <Option2> [| ...]";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public PollCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return $"@{message.DisplayName}, only mods can start polls.";
        }

        string args = message.Content.Length > Trigger.Length
            ? message.Content[(Trigger.Length + 1)..].Trim()
            : string.Empty;

        if (string.IsNullOrEmpty(args))
        {
            return $"@{message.DisplayName}, usage: !poll <seconds> <Question> | <Option1> | <Option2>";
        }

        string[] firstSplit = args.Split(' ', 2);
        if (firstSplit.Length < 2 || !int.TryParse(firstSplit[0], out int duration))
        {
            return $"@{message.DisplayName}, usage: !poll <seconds> <Question> | <Option1> | <Option2>";
        }

        string[] parts = firstSplit[1].Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return $"@{message.DisplayName}, need a question and at least 2 options separated by |";
        }

        string question = parts[0];
        string[] options = parts[1..];

        using IServiceScope scope = _scopeFactory.CreateScope();
        PollService pollService = scope.ServiceProvider.GetRequiredService<PollService>();

        PollResult result = await pollService.CreateBotPollAsync(
            question, options, duration, message.DisplayName, ct);

        if (!result.Success)
        {
            return $"@{message.DisplayName}, {result.Error}";
        }

        // PollService already announces in chat, so no response needed here
        return null;
    }
}
