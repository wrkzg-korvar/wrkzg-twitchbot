using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848, CA1873 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Manages poll lifecycle: create, vote, close, results.
/// Used by both chat commands and API endpoints.
/// </summary>
public class PollService
{
    private readonly IPollRepository _polls;
    private readonly IUserRepository _users;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ITwitchChatClient _chat;
    private readonly ISettingsRepository _settings;
    private readonly ILogger<PollService> _logger;

    public PollService(
        IPollRepository polls,
        IUserRepository users,
        IChatEventBroadcaster broadcaster,
        ITwitchChatClient chat,
        ISettingsRepository settings,
        ILogger<PollService> logger)
    {
        _polls = polls;
        _users = users;
        _broadcaster = broadcaster;
        _chat = chat;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Creates a new bot-native poll. Fails if another poll is already active.</summary>
    public async Task<PollResult> CreateBotPollAsync(
        string question,
        string[] options,
        int durationSeconds,
        string createdBy,
        CancellationToken ct = default)
    {
        if (options.Length < 2 || options.Length > 5)
        {
            return PollResult.Fail("Polls need 2-5 options.");
        }

        if (durationSeconds < 15 || durationSeconds > 1800)
        {
            return PollResult.Fail("Duration must be between 15 seconds and 30 minutes.");
        }

        Poll? existing = await _polls.GetActiveAsync(ct);
        if (existing is not null)
        {
            return PollResult.Fail("A poll is already active. Close it first with !pollend.");
        }

        Poll poll = new()
        {
            Question = question,
            Options = options,
            DurationSeconds = durationSeconds,
            EndsAt = DateTimeOffset.UtcNow.AddSeconds(durationSeconds),
            Source = PollSource.BotNative,
            CreatedBy = createdBy,
            IsActive = true
        };

        poll = await _polls.CreateAsync(poll, ct);

        await _broadcaster.BroadcastPollCreatedAsync(poll, ct);

        // Announce in chat using customizable template
        string optionList = string.Join(" ", options.Select((o, i) => $"[{i + 1}] {o}"));
        string template = await GetTemplateAsync(PollTemplates.AnnounceStartKey, PollTemplates.AnnounceStartDefault, ct);
        string announcement = template
            .Replace("{question}", question)
            .Replace("{options}", optionList)
            .Replace("{duration}", durationSeconds.ToString(CultureInfo.InvariantCulture));
        await SendChatMessageSafeAsync(announcement, ct);

        _logger.LogInformation("Bot poll #{Id} created by {User}: {Question}",
            poll.Id, createdBy, question);

        return PollResult.Ok(poll);
    }

    /// <summary>Registers a vote for the active poll.</summary>
    public async Task<VoteResult> VoteAsync(
        string twitchUserId,
        string username,
        int optionIndex,
        CancellationToken ct = default)
    {
        Poll? poll = await _polls.GetActiveAsync(ct);
        if (poll is null)
        {
            string noPollMsg = await GetTemplateAsync(PollTemplates.VoteNoPollKey, PollTemplates.VoteNoPollDefault, ct);
            return VoteResult.Fail(noPollMsg.Replace("{user}", username));
        }

        if (DateTimeOffset.UtcNow >= poll.EndsAt)
        {
            await ClosePollInternalAsync(poll, PollEndReason.TimerExpired, ct);
            return VoteResult.Fail("Poll has ended.");
        }

        if (optionIndex < 0 || optionIndex >= poll.Options.Length)
        {
            string invalidMsg = await GetTemplateAsync(PollTemplates.VoteInvalidKey, PollTemplates.VoteInvalidDefault, ct);
            return VoteResult.Fail(invalidMsg
                .Replace("{user}", username)
                .Replace("{max}", poll.Options.Length.ToString(CultureInfo.InvariantCulture)));
        }

        User user = await _users.GetOrCreateAsync(twitchUserId, username, ct);

        if (await _polls.HasUserVotedAsync(poll.Id, user.Id, ct))
        {
            string dupMsg = await GetTemplateAsync(PollTemplates.VoteDuplicateKey, PollTemplates.VoteDuplicateDefault, ct);
            return VoteResult.Fail(dupMsg.Replace("{user}", username));
        }

        PollVote vote = new()
        {
            PollId = poll.Id,
            UserId = user.Id,
            OptionIndex = optionIndex
        };

        await _polls.AddVoteAsync(vote, ct);

        await _broadcaster.BroadcastPollVoteAsync(poll.Id, optionIndex, ct);

        return VoteResult.Ok(poll.Options[optionIndex]);
    }

    /// <summary>Manually ends the active poll.</summary>
    public async Task<PollResult> EndPollAsync(PollEndReason reason = PollEndReason.ManuallyClosed, CancellationToken ct = default)
    {
        Poll? poll = await _polls.GetActiveAsync(ct);
        if (poll is null)
        {
            return PollResult.Fail("No active poll.");
        }

        await ClosePollInternalAsync(poll, reason, ct);
        return PollResult.Ok(poll);
    }

    /// <summary>Gets the results of a poll (active or ended).</summary>
    public async Task<PollResultsDto?> GetResultsAsync(int? pollId = null, CancellationToken ct = default)
    {
        Poll? poll = pollId.HasValue
            ? await _polls.GetByIdAsync(pollId.Value, ct)
            : await _polls.GetActiveAsync(ct);

        if (poll is null)
        {
            return null;
        }

        return BuildResults(poll);
    }

    /// <summary>Gets the currently active poll, or null.</summary>
    public Task<Poll?> GetActiveAsync(CancellationToken ct = default)
        => _polls.GetActiveAsync(ct);

    /// <summary>Gets recent poll history.</summary>
    public Task<IReadOnlyList<Poll>> GetRecentAsync(int count = 10, CancellationToken ct = default)
        => _polls.GetRecentAsync(count, ct);

    /// <summary>Checks and auto-closes expired polls. Called periodically.</summary>
    public async Task CheckExpiredPollsAsync(CancellationToken ct = default)
    {
        Poll? active = await _polls.GetActiveAsync(ct);
        if (active is not null && DateTimeOffset.UtcNow >= active.EndsAt)
        {
            await ClosePollInternalAsync(active, PollEndReason.TimerExpired, ct);
        }
    }

    // ─── Private ─────────────────────────────────────────

    private async Task ClosePollInternalAsync(Poll poll, PollEndReason reason, CancellationToken ct)
    {
        poll.IsActive = false;
        poll.EndReason = reason;
        await _polls.UpdateAsync(poll, ct);

        PollResultsDto results = BuildResults(poll);
        await _broadcaster.BroadcastPollEndedAsync(results, ct);

        // Announce results in chat using customizable templates
        if (reason == PollEndReason.Cancelled)
        {
            string cancelTemplate = await GetTemplateAsync(PollTemplates.AnnounceCancelKey, PollTemplates.AnnounceCancelDefault, ct);
            string cancelMsg = cancelTemplate.Replace("{question}", results.Question);
            await SendChatMessageSafeAsync(cancelMsg, ct);
        }
        else if (results.WinnerIndex.HasValue)
        {
            PollOptionResult winnerOpt = results.Options[results.WinnerIndex.Value];
            string endTemplate = await GetTemplateAsync(PollTemplates.AnnounceEndKey, PollTemplates.AnnounceEndDefault, ct);
            string endMsg = endTemplate
                .Replace("{question}", results.Question)
                .Replace("{winner}", winnerOpt.Label)
                .Replace("{winner_votes}", winnerOpt.Votes.ToString(CultureInfo.InvariantCulture))
                .Replace("{winner_percent}", winnerOpt.Percentage.ToString(CultureInfo.InvariantCulture))
                .Replace("{total_votes}", results.TotalVotes.ToString(CultureInfo.InvariantCulture));
            await SendChatMessageSafeAsync(endMsg, ct);
        }
        else
        {
            string noVotesTemplate = await GetTemplateAsync(PollTemplates.AnnounceNoVotesKey, PollTemplates.AnnounceNoVotesDefault, ct);
            string noVotesMsg = noVotesTemplate.Replace("{question}", results.Question);
            await SendChatMessageSafeAsync(noVotesMsg, ct);
        }

        _logger.LogInformation("Poll #{Id} ended ({Reason}): {Question}", poll.Id, reason, poll.Question);
    }

    private async Task<string> GetTemplateAsync(string key, string defaultValue, CancellationToken ct)
    {
        string? custom = await _settings.GetAsync(key, ct);
        return string.IsNullOrEmpty(custom) ? defaultValue : custom;
    }

    private async Task SendChatMessageSafeAsync(string message, CancellationToken ct)
    {
        try
        {
            if (_chat.IsConnected)
            {
                await _chat.SendMessageAsync(message, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send poll chat announcement");
        }
    }

    private static PollResultsDto BuildResults(Poll poll)
    {
        int totalVotes = poll.Votes.Count;
        PollOptionResult[] optionResults = poll.Options.Select((opt, idx) =>
        {
            int votes = poll.Votes.Count(v => v.OptionIndex == idx);
            double percentage = totalVotes > 0 ? Math.Round((double)votes / totalVotes * 100, 1) : 0;
            return new PollOptionResult(idx, opt, votes, percentage);
        }).ToArray();

        int? winnerIndex = totalVotes > 0
            ? optionResults.OrderByDescending(o => o.Votes).First().Index
            : null;

        return new PollResultsDto(
            poll.Id,
            poll.Question,
            poll.IsActive,
            poll.Source,
            poll.CreatedBy,
            poll.CreatedAt,
            poll.EndsAt,
            poll.EndReason,
            totalVotes,
            optionResults,
            winnerIndex);
    }
}

// ─── Result Types ────────────────────────────────────────

public record PollResult(bool Success, string? Error, Poll? Poll)
{
    public static PollResult Ok(Poll poll) => new(true, null, poll);
    public static PollResult Fail(string error) => new(false, error, null);
}

public record VoteResult(bool Success, string? Error, string? OptionText)
{
    public static VoteResult Ok(string optionText) => new(true, null, optionText);
    public static VoteResult Fail(string error) => new(false, error, null);
}

public record PollOptionResult(int Index, string Label, int Votes, double Percentage);

public record PollResultsDto(
    int Id,
    string Question,
    bool IsActive,
    PollSource Source,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset EndsAt,
    PollEndReason EndReason,
    int TotalVotes,
    PollOptionResult[] Options,
    int? WinnerIndex);
