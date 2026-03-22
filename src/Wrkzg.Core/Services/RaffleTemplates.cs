using System.Collections.Generic;

namespace Wrkzg.Core.Services;

/// <summary>
/// Raffle announcement template keys, defaults, and metadata.
/// </summary>
public static class RaffleTemplates
{
    public static readonly Dictionary<string, string> Defaults = new()
    {
        ["raffle.announce.start"] = "\ud83c\udf89 RAFFLE: {title} \u2014 {join_method} to enter! ({duration})",
        ["raffle.announce.winner"] = "\ud83c\udf89 Congratulations @{winner}! You won the raffle: {title} ({total_entries} entries)",
        ["raffle.announce.cancel"] = "\ud83c\udf89 Raffle cancelled: {title}",
        ["raffle.announce.no_entries"] = "\ud83c\udf89 Raffle ended with no entries: {title}",
        ["raffle.entry.duplicate"] = "@{user}, you already entered this raffle.",
        ["raffle.entry.no_raffle"] = "@{user}, no active raffle.",
        ["raffle.entry.closed"] = "@{user}, entries are closed.",
        ["raffle.entry.success"] = "",
    };

    public static readonly Dictionary<string, string> Descriptions = new()
    {
        ["raffle.announce.start"] = "Sent in chat when a raffle starts",
        ["raffle.announce.winner"] = "Sent in chat when a winner is drawn",
        ["raffle.announce.cancel"] = "Sent in chat when a raffle is cancelled",
        ["raffle.announce.no_entries"] = "Sent when a raffle has no entries",
        ["raffle.entry.duplicate"] = "Sent when a user tries to enter twice",
        ["raffle.entry.no_raffle"] = "Sent when no raffle is active",
        ["raffle.entry.closed"] = "Sent when entries are closed",
        ["raffle.entry.success"] = "Sent on successful entry (empty = silent)",
    };

    public static readonly Dictionary<string, string[]> Variables = new()
    {
        ["raffle.announce.start"] = new[] { "title", "keyword", "join_method", "max_entries", "duration" },
        ["raffle.announce.winner"] = new[] { "title", "winner", "total_entries" },
        ["raffle.announce.cancel"] = new[] { "title" },
        ["raffle.announce.no_entries"] = new[] { "title" },
        ["raffle.entry.duplicate"] = new[] { "user" },
        ["raffle.entry.no_raffle"] = new[] { "user" },
        ["raffle.entry.closed"] = new[] { "user" },
        ["raffle.entry.success"] = new[] { "user", "entry_count" },
    };
}
