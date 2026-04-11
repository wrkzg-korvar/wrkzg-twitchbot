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
/// Manages raffle lifecycle: create, enter, draw, cancel.
/// Used by both chat commands and API endpoints.
/// </summary>
public class RaffleService
{
    private readonly IRaffleRepository _raffles;
    private readonly IUserRepository _users;
    private readonly ISettingsRepository _settings;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ITwitchChatClient _chat;
    private readonly ILogger<RaffleService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RaffleService"/> with the required dependencies.
    /// </summary>
    /// <param name="raffles">Repository for raffle persistence.</param>
    /// <param name="users">Repository for user lookup and creation.</param>
    /// <param name="settings">Repository for customizable template settings.</param>
    /// <param name="broadcaster">Broadcasts real-time raffle events to the dashboard.</param>
    /// <param name="chat">The Twitch IRC chat client for sending announcements.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public RaffleService(
        IRaffleRepository raffles,
        IUserRepository users,
        ISettingsRepository settings,
        IChatEventBroadcaster broadcaster,
        ITwitchChatClient chat,
        ILogger<RaffleService> logger)
    {
        _raffles = raffles;
        _users = users;
        _settings = settings;
        _broadcaster = broadcaster;
        _chat = chat;
        _logger = logger;
    }

    /// <summary>Creates a new raffle. Fails if another raffle is already open.</summary>
    public async Task<RaffleResult> CreateAsync(
        string title,
        string? keyword,
        int? durationSeconds,
        int? maxEntries,
        string createdBy,
        CancellationToken ct = default)
    {
        Raffle? existing = await _raffles.GetActiveAsync(ct);
        if (existing is not null)
        {
            return RaffleResult.Fail("A raffle is already open. Close it first.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return RaffleResult.Fail("Raffle needs a title.");
        }

        if (keyword is not null)
        {
            keyword = keyword.Trim().ToLowerInvariant();
            if (keyword.StartsWith('!'))
            {
                return RaffleResult.Fail("Keyword should not start with '!' \u2014 it's a chat word, not a command.");
            }
            if (keyword.Contains(' '))
            {
                return RaffleResult.Fail("Keyword must be a single word.");
            }
        }

        Raffle raffle = new()
        {
            Title = title,
            Keyword = keyword,
            DurationSeconds = durationSeconds,
            EntriesCloseAt = durationSeconds.HasValue
                ? DateTimeOffset.UtcNow.AddSeconds(durationSeconds.Value)
                : null,
            MaxEntries = maxEntries,
            CreatedBy = createdBy,
            IsOpen = true
        };

        raffle = await _raffles.CreateAsync(raffle, ct);

        await _broadcaster.BroadcastRaffleCreatedAsync(raffle, ct);

        string template = await GetTemplateAsync("raffle.announce.start",
            RaffleTemplates.Defaults["raffle.announce.start"], ct);
        string joinMethod = !string.IsNullOrEmpty(keyword)
            ? $"type \"{keyword}\""
            : "type !join";
        string announcement = template
            .Replace("{title}", title)
            .Replace("{keyword}", keyword ?? "!join")
            .Replace("{join_method}", joinMethod)
            .Replace("{max_entries}", maxEntries?.ToString(CultureInfo.InvariantCulture) ?? "unlimited")
            .Replace("{duration}", durationSeconds?.ToString(CultureInfo.InvariantCulture) ?? "until drawn");
        await SendChatSafeAsync(announcement, ct);

        _logger.LogInformation("Raffle #{Id} created by {User}: {Title} (keyword: {Keyword})",
            raffle.Id, createdBy, title, keyword ?? "!join");

        return RaffleResult.Ok(raffle);
    }

    /// <summary>Registers a user entry for the active raffle.</summary>
    public async Task<EntryResult> EnterAsync(
        string twitchUserId,
        string username,
        CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            string msg = await GetTemplateAsync("raffle.entry.no_raffle",
                RaffleTemplates.Defaults["raffle.entry.no_raffle"], ct);
            return EntryResult.Fail(msg, false);
        }

        if (raffle.EntriesCloseAt.HasValue && DateTimeOffset.UtcNow >= raffle.EntriesCloseAt.Value)
        {
            string msg = await GetTemplateAsync("raffle.entry.closed",
                RaffleTemplates.Defaults["raffle.entry.closed"], ct);
            return EntryResult.Fail(msg, false);
        }

        if (raffle.MaxEntries.HasValue)
        {
            int currentCount = await _raffles.GetEntryCountAsync(raffle.Id, ct);
            if (currentCount >= raffle.MaxEntries.Value)
            {
                return EntryResult.Fail("Raffle is full.", false);
            }
        }

        User user = await _users.GetOrCreateAsync(twitchUserId, username, ct);

        if (await _raffles.HasUserEnteredAsync(raffle.Id, user.Id, ct))
        {
            string msg = await GetTemplateAsync("raffle.entry.duplicate",
                RaffleTemplates.Defaults["raffle.entry.duplicate"], ct);
            return EntryResult.Fail(msg, true);
        }

        RaffleEntry entry = new()
        {
            RaffleId = raffle.Id,
            UserId = user.Id,
            TicketCount = 1
        };

        await _raffles.AddEntryAsync(entry, ct);

        int entryCount = await _raffles.GetEntryCountAsync(raffle.Id, ct);

        await _broadcaster.BroadcastRaffleEntryAsync(raffle.Id, username, entryCount, ct);

        // Send entry confirmation if template is configured (empty = silent)
        string successTemplate = await GetTemplateAsync("raffle.entry.success",
            RaffleTemplates.Defaults["raffle.entry.success"], ct);
        if (!string.IsNullOrWhiteSpace(successTemplate))
        {
            string confirmation = successTemplate
                .Replace("{user}", username)
                .Replace("{entry_count}", entryCount.ToString(CultureInfo.InvariantCulture));
            await SendChatSafeAsync(confirmation, ct);
        }

        return EntryResult.Ok(entryCount);
    }

    /// <summary>
    /// Draws a random winner from the active raffle.
    /// Sets PendingWinnerId but does NOT close the raffle.
    /// The streamer must call AcceptWinnerAsync or RedrawAsync.
    /// </summary>
    public async Task<DrawResult> DrawAsync(CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            return DrawResult.Fail("No active raffle.");
        }

        if (raffle.Entries.Count == 0)
        {
            return DrawResult.Fail("No entries in the raffle.");
        }

        if (raffle.PendingWinnerId is not null)
        {
            return DrawResult.Fail("A draw is already pending verification. Accept or redraw first.");
        }

        // Exclude previously drawn users
        HashSet<int> drawnUserIds = raffle.Draws.Select(d => d.UserId).ToHashSet();
        List<RaffleEntry> eligibleEntries = raffle.Entries
            .Where(e => !drawnUserIds.Contains(e.UserId))
            .ToList();

        if (eligibleEntries.Count == 0)
        {
            return DrawResult.Fail("All entries have been drawn already.");
        }

        RaffleEntry winnerEntry = eligibleEntries[Random.Shared.Next(eligibleEntries.Count)];
        string winnerName = winnerEntry.User?.DisplayName ?? "Unknown";
        string winnerTwitchId = winnerEntry.User?.TwitchId ?? "";

        int drawNumber = raffle.Draws.Count + 1;
        RaffleDraw draw = new()
        {
            RaffleId = raffle.Id,
            UserId = winnerEntry.UserId,
            DrawNumber = drawNumber,
            IsAccepted = false
        };
        await _raffles.AddDrawAsync(draw, ct);

        raffle.PendingWinnerId = winnerEntry.UserId;
        await _raffles.UpdateAsync(raffle, ct);

        await _broadcaster.BroadcastRaffleDrawPendingAsync(
            raffle.Id, winnerName, winnerTwitchId, raffle.Entries.Count, drawNumber, ct);

        _logger.LogInformation("Raffle #{Id} draw #{DrawNumber} \u2014 pending winner: {Winner}",
            raffle.Id, drawNumber, winnerName);

        return DrawResult.Ok(winnerName, raffle.Entries.Count);
    }

    /// <summary>
    /// Accepts the pending winner. Raffle stays OPEN for potential additional draws.
    /// Use EndRaffleAsync to close the raffle.
    /// </summary>
    public async Task<DrawResult> AcceptWinnerAsync(CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            return DrawResult.Fail("No active raffle.");
        }
        if (raffle.PendingWinnerId is null)
        {
            return DrawResult.Fail("No pending winner to accept.");
        }

        RaffleDraw? lastDraw = raffle.Draws
            .OrderByDescending(d => d.DrawNumber)
            .FirstOrDefault(d => d.UserId == raffle.PendingWinnerId);
        string winnerName = lastDraw?.User?.DisplayName ?? raffle.PendingWinner?.DisplayName ?? "Unknown";
        int drawNumber = lastDraw?.DrawNumber ?? 0;

        if (lastDraw is not null)
        {
            lastDraw.IsAccepted = true;
        }

        // Clear pending but keep raffle OPEN for potential additional draws
        raffle.PendingWinnerId = null;
        await _raffles.UpdateAsync(raffle, ct);

        await _broadcaster.BroadcastRaffleWinnerAcceptedAsync(raffle.Id, winnerName, drawNumber, ct);

        _logger.LogInformation("Raffle #{Id} winner accepted: {Winner} (draw #{DrawNumber})",
            raffle.Id, winnerName, drawNumber);

        return DrawResult.Ok(winnerName, raffle.Entries.Count);
    }

    /// <summary>
    /// Closes the raffle. Sends winner announcement(s) in chat.
    /// If there's a pending winner, it gets rejected.
    /// </summary>
    public async Task<RaffleResult> EndRaffleAsync(CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            return RaffleResult.Fail("No active raffle.");
        }

        // If there's a pending winner, reject it
        if (raffle.PendingWinnerId is not null)
        {
            RaffleDraw? pendingDraw = raffle.Draws
                .OrderByDescending(d => d.DrawNumber)
                .FirstOrDefault(d => d.UserId == raffle.PendingWinnerId);
            if (pendingDraw is not null)
            {
                pendingDraw.RedrawReason = "Raffle ended";
            }
            raffle.PendingWinnerId = null;
        }

        // Set winner to the last accepted draw's user (if any)
        RaffleDraw? lastAccepted = raffle.Draws
            .Where(d => d.IsAccepted)
            .OrderByDescending(d => d.DrawNumber)
            .FirstOrDefault();
        if (lastAccepted is not null)
        {
            raffle.WinnerId = lastAccepted.UserId;
        }

        raffle.IsOpen = false;
        raffle.ClosedAt = DateTimeOffset.UtcNow;
        raffle.EndReason = RaffleEndReason.Drawn;
        await _raffles.UpdateAsync(raffle, ct);

        // Send chat announcement for all accepted winners
        List<string> acceptedNames = raffle.Draws
            .Where(d => d.IsAccepted)
            .Select(d => d.User?.DisplayName ?? "Unknown")
            .ToList();

        if (acceptedNames.Count > 0)
        {
            string template = await GetTemplateAsync("raffle.announce.winner",
                RaffleTemplates.Defaults["raffle.announce.winner"], ct);
            string winners = string.Join(", ", acceptedNames);
            string announcement = template
                .Replace("{title}", raffle.Title)
                .Replace("{winner}", winners)
                .Replace("{total_entries}", raffle.Entries.Count.ToString(CultureInfo.InvariantCulture));
            await SendChatSafeAsync(announcement, ct);
        }

        await _broadcaster.BroadcastRaffleEndedAsync(raffle.Id, ct);

        _logger.LogInformation("Raffle #{Id} ended with {WinnerCount} winner(s)",
            raffle.Id, acceptedNames.Count);

        return RaffleResult.Ok(raffle);
    }

    /// <summary>
    /// Rejects the current pending winner and draws a new one.
    /// The rejected user will not be drawn again.
    /// </summary>
    public async Task<DrawResult> RedrawAsync(string? reason = null, CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            return DrawResult.Fail("No active raffle.");
        }
        if (raffle.PendingWinnerId is null)
        {
            return DrawResult.Fail("No pending winner to redraw.");
        }

        // Pre-check: verify there are eligible entries BEFORE clearing pending winner
        HashSet<int> drawnUserIds = raffle.Draws.Select(d => d.UserId).ToHashSet();
        drawnUserIds.Add(raffle.PendingWinnerId.Value);
        int eligibleCount = raffle.Entries.Count(e => !drawnUserIds.Contains(e.UserId));

        if (eligibleCount == 0)
        {
            return DrawResult.Fail("No more eligible entries available for redraw. Accept the current winner or end the raffle.");
        }

        RaffleDraw? lastDraw = raffle.Draws
            .OrderByDescending(d => d.DrawNumber)
            .FirstOrDefault(d => d.UserId == raffle.PendingWinnerId);
        if (lastDraw is not null)
        {
            lastDraw.RedrawReason = reason ?? "User not present";
        }

        raffle.PendingWinnerId = null;
        await _raffles.UpdateAsync(raffle, ct);

        _logger.LogInformation("Raffle #{Id} redraw \u2014 rejected user, reason: {Reason}",
            raffle.Id, reason ?? "User not present");

        return await DrawAsync(ct);
    }

    /// <summary>Cancels the active raffle without drawing.</summary>
    public async Task<RaffleResult> CancelAsync(CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            return RaffleResult.Fail("No active raffle.");
        }

        raffle.IsOpen = false;
        raffle.ClosedAt = DateTimeOffset.UtcNow;
        raffle.EndReason = RaffleEndReason.Cancelled;
        await _raffles.UpdateAsync(raffle, ct);

        await _broadcaster.BroadcastRaffleCancelledAsync(raffle.Id, ct);

        string template = await GetTemplateAsync("raffle.announce.cancel",
            RaffleTemplates.Defaults["raffle.announce.cancel"], ct);
        string announcement = template.Replace("{title}", raffle.Title);
        await SendChatSafeAsync(announcement, ct);

        return RaffleResult.Ok(raffle);
    }

    /// <summary>
    /// Checks if the active raffle timer has expired and auto-draws a winner.
    /// Called periodically by RaffleTimerService.
    /// Returns true if there is an active raffle (for adaptive polling interval).
    /// </summary>
    public async Task<bool> CheckExpiredRafflesAsync(CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null)
        {
            return false;
        }
        if (!raffle.EntriesCloseAt.HasValue)
        {
            return true; // Active raffle without timer
        }
        if (DateTimeOffset.UtcNow < raffle.EntriesCloseAt.Value)
        {
            return true; // Active raffle, timer not yet expired
        }
        if (raffle.PendingWinnerId is not null)
        {
            return true; // Already drawn, waiting for verification
        }
        if (raffle.Draws.Any(d => d.IsAccepted))
        {
            return true; // Has accepted draws — streamer is managing manually
        }

        if (raffle.Entries.Count > 0)
        {
            await DrawAsync(ct);
        }
        else
        {
            raffle.IsOpen = false;
            raffle.ClosedAt = DateTimeOffset.UtcNow;
            raffle.EndReason = RaffleEndReason.Cancelled;
            await _raffles.UpdateAsync(raffle, ct);

            string template = await GetTemplateAsync("raffle.announce.no_entries",
                RaffleTemplates.Defaults["raffle.announce.no_entries"], ct);
            string announcement = template.Replace("{title}", raffle.Title);
            await SendChatSafeAsync(announcement, ct);

            await _broadcaster.BroadcastRaffleCancelledAsync(raffle.Id, ct);
        }

        return false; // Raffle just ended
    }

    /// <summary>Gets the currently active raffle with entries.</summary>
    public Task<Raffle?> GetActiveAsync(CancellationToken ct = default)
        => _raffles.GetActiveAsync(ct);

    /// <summary>Gets a raffle by ID with entries.</summary>
    public Task<Raffle?> GetByIdAsync(int id, CancellationToken ct = default)
        => _raffles.GetByIdAsync(id, ct);

    /// <summary>Gets recent raffle history.</summary>
    public Task<IReadOnlyList<Raffle>> GetRecentAsync(int count = 10, CancellationToken ct = default)
        => _raffles.GetRecentAsync(count, ct);

    /// <summary>
    /// Handles an incoming chat message to check for keyword entry.
    /// Returns true if the message was a keyword entry.
    /// </summary>
    public async Task<bool> TryKeywordEntryAsync(
        string twitchUserId,
        string username,
        string messageContent,
        CancellationToken ct = default)
    {
        Raffle? raffle = await _raffles.GetActiveAsync(ct);
        if (raffle is null || string.IsNullOrEmpty(raffle.Keyword))
        {
            return false;
        }

        if (!string.Equals(messageContent.Trim(), raffle.Keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        await EnterAsync(twitchUserId, username, ct);
        return true;
    }

    // ─── Private Helpers ─────────────────────────────────

    private async Task<string> GetTemplateAsync(string key, string defaultValue, CancellationToken ct)
    {
        string? custom = await _settings.GetAsync(key, ct);
        return string.IsNullOrWhiteSpace(custom) ? defaultValue : custom;
    }

    private async Task SendChatSafeAsync(string message, CancellationToken ct)
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
            _logger.LogWarning(ex, "Failed to send raffle chat announcement");
        }
    }
}

// ─── Result Types ────────────────────────────────────────

/// <summary>
/// Result of a raffle creation, closure, or cancellation operation.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Error">Error message if the operation failed; null on success.</param>
/// <param name="Raffle">The raffle entity if the operation succeeded; null on failure.</param>
public record RaffleResult(bool Success, string? Error, Raffle? Raffle)
{
    /// <summary>Creates a successful raffle result.</summary>
    /// <param name="raffle">The raffle entity.</param>
    /// <returns>A successful <see cref="RaffleResult"/>.</returns>
    public static RaffleResult Ok(Raffle raffle) => new(true, null, raffle);

    /// <summary>Creates a failed raffle result with the given error message.</summary>
    /// <param name="error">Description of why the operation failed.</param>
    /// <returns>A failed <see cref="RaffleResult"/>.</returns>
    public static RaffleResult Fail(string error) => new(false, error, null);
}

/// <summary>
/// Result of a raffle entry attempt, indicating whether the user was added.
/// </summary>
/// <param name="Success">Whether the entry was accepted.</param>
/// <param name="Error">Error message if the entry failed; null on success.</param>
/// <param name="AlreadyEntered">Whether the user had already entered this raffle.</param>
/// <param name="EntryCount">Total number of entries in the raffle after this attempt.</param>
public record EntryResult(bool Success, string? Error, bool AlreadyEntered, int EntryCount = 0)
{
    /// <summary>Creates a successful entry result.</summary>
    /// <param name="entryCount">Total entries in the raffle after the new entry.</param>
    /// <returns>A successful <see cref="EntryResult"/>.</returns>
    public static EntryResult Ok(int entryCount) => new(true, null, false, entryCount);

    /// <summary>Creates a failed entry result with the given error message.</summary>
    /// <param name="error">Description of why the entry failed.</param>
    /// <param name="alreadyEntered">Whether the failure was due to a duplicate entry.</param>
    /// <returns>A failed <see cref="EntryResult"/>.</returns>
    public static EntryResult Fail(string error, bool alreadyEntered) => new(false, error, alreadyEntered, 0);
}

/// <summary>
/// Result of a raffle draw operation, containing the winner information.
/// </summary>
/// <param name="Success">Whether the draw succeeded.</param>
/// <param name="Error">Error message if the draw failed; null on success.</param>
/// <param name="WinnerName">Display name of the drawn winner; null on failure.</param>
/// <param name="TotalEntries">Total number of entries in the raffle.</param>
public record DrawResult(bool Success, string? Error, string? WinnerName, int TotalEntries = 0)
{
    /// <summary>Creates a successful draw result.</summary>
    /// <param name="winner">Display name of the winner.</param>
    /// <param name="entries">Total entries in the raffle.</param>
    /// <returns>A successful <see cref="DrawResult"/>.</returns>
    public static DrawResult Ok(string winner, int entries) => new(true, null, winner, entries);

    /// <summary>Creates a failed draw result with the given error message.</summary>
    /// <param name="error">Description of why the draw failed.</param>
    /// <returns>A failed <see cref="DrawResult"/>.</returns>
    public static DrawResult Fail(string error) => new(false, error, null, 0);
}
