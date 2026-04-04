using System.Collections.Generic;

namespace Wrkzg.Core.Services;

/// <summary>
/// Poll announcement template keys and their default values.
/// Templates use {variable} placeholders resolved at runtime.
/// </summary>
public static class PollTemplates
{
    // ─── Keys ────────────────────────────────────────────

    /// <summary>Settings key for the poll start announcement template.</summary>
    public const string AnnounceStartKey = "poll.announce.start";

    /// <summary>Settings key for the poll end (with votes) announcement template.</summary>
    public const string AnnounceEndKey = "poll.announce.end";

    /// <summary>Settings key for the poll cancellation announcement template.</summary>
    public const string AnnounceCancelKey = "poll.announce.cancel";

    /// <summary>Settings key for the poll end (no votes) announcement template.</summary>
    public const string AnnounceNoVotesKey = "poll.announce.no_votes";

    /// <summary>Settings key for the duplicate vote response template.</summary>
    public const string VoteDuplicateKey = "poll.vote.duplicate";

    /// <summary>Settings key for the "no active poll" response template.</summary>
    public const string VoteNoPollKey = "poll.vote.no_poll";

    /// <summary>Settings key for the invalid vote option response template.</summary>
    public const string VoteInvalidKey = "poll.vote.invalid";

    // ─── Defaults ────────────────────────────────────────

    /// <summary>Default template for announcing a poll start. Variables: {question}, {options}, {duration}.</summary>
    public const string AnnounceStartDefault =
        "\ud83d\udcca POLL: {question} \u2014 {options} \u2014 Vote with !vote <number> ({duration}s)";

    /// <summary>Default template for announcing poll results. Variables: {question}, {winner}, {winner_votes}, {winner_percent}, {total_votes}.</summary>
    public const string AnnounceEndDefault =
        "\ud83d\udcca Poll ended! {question} \u2014 Winner: {winner} ({winner_votes} votes, {winner_percent}%) \u2014 {total_votes} total votes";

    /// <summary>Default template for announcing a poll cancellation. Variables: {question}.</summary>
    public const string AnnounceCancelDefault =
        "\ud83d\udcca Poll cancelled: {question}";

    /// <summary>Default template for announcing a poll that ended with no votes. Variables: {question}.</summary>
    public const string AnnounceNoVotesDefault =
        "\ud83d\udcca Poll ended! {question} \u2014 No votes were cast.";

    /// <summary>Default response when a user tries to vote twice. Variables: {user}.</summary>
    public const string VoteDuplicateDefault =
        "@{user}, you already voted.";

    /// <summary>Default response when no poll is active. Variables: {user}.</summary>
    public const string VoteNoPollDefault =
        "@{user}, no active poll.";

    /// <summary>Default response for an invalid vote option number. Variables: {user}, {max}.</summary>
    public const string VoteInvalidDefault =
        "@{user}, invalid option. Vote 1-{max}.";

    // ─── Registry ────────────────────────────────────────

    /// <summary>All template definitions with key, default value, description, and available variables.</summary>
    public static readonly IReadOnlyList<PollTemplateDefinition> All = new[]
    {
        new PollTemplateDefinition(AnnounceStartKey, AnnounceStartDefault,
            "Sent to chat when a poll starts.",
            new[] { "question", "options", "duration" }),

        new PollTemplateDefinition(AnnounceEndKey, AnnounceEndDefault,
            "Sent to chat when a poll ends with votes.",
            new[] { "question", "winner", "winner_votes", "winner_percent", "total_votes" }),

        new PollTemplateDefinition(AnnounceCancelKey, AnnounceCancelDefault,
            "Sent to chat when a poll is cancelled.",
            new[] { "question" }),

        new PollTemplateDefinition(AnnounceNoVotesKey, AnnounceNoVotesDefault,
            "Sent to chat when a poll ends with no votes.",
            new[] { "question" }),

        new PollTemplateDefinition(VoteDuplicateKey, VoteDuplicateDefault,
            "Shown when a user tries to vote twice.",
            new[] { "user" }),

        new PollTemplateDefinition(VoteNoPollKey, VoteNoPollDefault,
            "Shown when a user votes but no poll is active.",
            new[] { "user" }),

        new PollTemplateDefinition(VoteInvalidKey, VoteInvalidDefault,
            "Shown when a user votes with an invalid option number.",
            new[] { "user", "max" }),
    };
}

/// <summary>
/// A poll template definition with metadata for the API and frontend.
/// </summary>
public record PollTemplateDefinition(
    string Key,
    string Default,
    string Description,
    string[] Variables);
