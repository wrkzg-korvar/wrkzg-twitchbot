using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for moderation event log entries.
/// Append-only by design — events are created but never individually updated.
/// </summary>
public interface IModerationEventRepository
{
    /// <summary>Creates a new moderation event log entry.</summary>
    Task<ModerationEvent> CreateAsync(ModerationEvent evt, CancellationToken ct = default);

    /// <summary>Gets moderation events for a specific user, newest first.</summary>
    Task<IReadOnlyList<ModerationEvent>> GetByUserAsync(string twitchUserId, int limit = 50, CancellationToken ct = default);

    /// <summary>Gets the most recent moderation events across all users.</summary>
    Task<IReadOnlyList<ModerationEvent>> GetRecentAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>Deletes all events older than the specified date. Returns the number of deleted rows.</summary>
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
