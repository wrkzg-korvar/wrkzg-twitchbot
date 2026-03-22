using System.Collections.Generic;

namespace Wrkzg.Core.Services;

/// <summary>
/// Poll announcement template keys and their default values.
/// Templates use {variable} placeholders resolved at runtime.
/// </summary>
public static class PollTemplates
{
    // ─── Keys ────────────────────────────────────────────

    public const string AnnounceStartKey = "poll.announce.start";
    public const string AnnounceEndKey = "poll.announce.end";
    public const string AnnounceCancelKey = "poll.announce.cancel";
    public const string AnnounceNoVotesKey = "poll.announce.no_votes";
    public const string VoteDuplicateKey = "poll.vote.duplicate";
    public const string VoteNoPollKey = "poll.vote.no_poll";
    public const string VoteInvalidKey = "poll.vote.invalid";

    // ─── Defaults ────────────────────────────────────────

    public const string AnnounceStartDefault =
        "\ud83d\udcca POLL: {question} \u2014 {options} \u2014 Vote with !vote <number> ({duration}s)";

    public const string AnnounceEndDefault =
        "\ud83d\udcca Poll ended! {question} \u2014 Winner: {winner} ({winner_votes} votes, {winner_percent}%) \u2014 {total_votes} total votes";

    public const string AnnounceCancelDefault =
        "\ud83d\udcca Poll cancelled: {question}";

    public const string AnnounceNoVotesDefault =
        "\ud83d\udcca Poll ended! {question} \u2014 No votes were cast.";

    public const string VoteDuplicateDefault =
        "@{user}, you already voted.";

    public const string VoteNoPollDefault =
        "@{user}, no active poll.";

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
