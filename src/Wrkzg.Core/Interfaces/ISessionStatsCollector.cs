namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Collects per-session statistics (messages, unique chatters, follows, subscriptions)
/// from different parts of the system. The StreamAnalyticsService reads and resets
/// the counters when a stream session ends.
/// </summary>
public interface ISessionStatsCollector
{
    /// <summary>Records a chat message from a specific user. Thread-safe.</summary>
    /// <param name="userId">The Twitch user ID of the message sender.</param>
    void RecordChatMessage(string userId);

    /// <summary>Records a new follower event. Thread-safe.</summary>
    void RecordFollow();

    /// <summary>Records one or more new subscription events. Thread-safe.</summary>
    /// <param name="count">Number of subscriptions (1 for a single sub, N for gift subs).</param>
    void RecordSubscription(int count = 1);

    /// <summary>
    /// Returns the accumulated stats since the last reset and clears all counters.
    /// Called by StreamAnalyticsService when a session ends.
    /// </summary>
    /// <returns>The accumulated session statistics.</returns>
    SessionStats GetAndResetStats();
}

/// <summary>
/// Snapshot of per-session statistics collected between session open and close.
/// </summary>
public sealed class SessionStats
{
    /// <summary>Number of unique users who sent at least one message.</summary>
    public int UniqueChatters { get; init; }

    /// <summary>Total chat messages received during the session.</summary>
    public int TotalMessages { get; init; }

    /// <summary>Number of new followers received during the session.</summary>
    public int NewFollowers { get; init; }

    /// <summary>Number of new subscriptions (including gifts) during the session.</summary>
    public int NewSubscribers { get; init; }
}
