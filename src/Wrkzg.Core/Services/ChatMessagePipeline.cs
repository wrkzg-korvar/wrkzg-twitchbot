using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Processes every incoming chat message through the command and game pipeline.
///
/// Called by BotConnectionService for each message received from IRC.
/// Runs in order:
///   1. Update user stats (message count, last seen)
///   2. Try CommandProcessor (custom commands like !discord, !socials)
///   3. Try ChatGameManager (active games like !duel, !slots) — future step
///
/// Uses IServiceScopeFactory to resolve Scoped dependencies (repositories)
/// because this service is Singleton (registered via BotConnectionService).
/// </summary>
public class ChatMessagePipeline
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IUserTrackingService _tracking;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatMessagePipeline> _logger;

    public ChatMessagePipeline(
        ICommandProcessor commandProcessor,
        IUserTrackingService tracking,
        IServiceScopeFactory scopeFactory,
        ILogger<ChatMessagePipeline> logger)
    {
        _commandProcessor = commandProcessor;
        _tracking = tracking;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Processes a single chat message through the full pipeline.
    /// </summary>
    public async Task ProcessAsync(ChatMessage message, CancellationToken ct = default)
    {
        try
        {
            // 1. Update user stats (scoped — needs DB access)
            await UpdateUserStatsAsync(message, ct);

            // Mark user active for watch time tracking
            _tracking.MarkUserActive(message.UserId);

            // 2. Check for raffle keyword entry (before command processing)
            {
                using IServiceScope raffleScope = _scopeFactory.CreateScope();
                RaffleService raffleService = raffleScope.ServiceProvider.GetRequiredService<RaffleService>();
                bool wasKeywordEntry = await raffleService.TryKeywordEntryAsync(
                    message.UserId, message.Username, message.Content, ct);
                if (wasKeywordEntry)
                {
                    return;
                }
            }

            // 3. Try custom commands
            bool handled = await _commandProcessor.HandleMessageAsync(message, ct);

            if (handled)
            {
                return;
            }

            // 3. Future: try chat games
            // bool gameHandled = await _chatGameManager.HandleMessageAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message from {User}: {Content}",
                message.Username, TruncateForLog(message.Content));
        }
    }

    /// <summary>
    /// Increments the user's message count and updates LastSeenAt.
    /// Uses a scoped service provider for DB access.
    /// </summary>
    private async Task UpdateUserStatsAsync(ChatMessage message, CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            User user = await users.GetOrCreateAsync(message.UserId, message.Username, ct);

            user.MessageCount++;
            user.LastSeenAt = DateTimeOffset.UtcNow;
            user.DisplayName = message.DisplayName; // Keep display name in sync
            user.IsMod = message.IsModerator;
            user.IsSubscriber = message.IsSubscriber;
            user.IsBroadcaster = message.IsBroadcaster;

            await users.UpdateAsync(user, ct);
        }
        catch (Exception ex)
        {
            // Don't let stats tracking failure break the command pipeline
            _logger.LogWarning(ex, "Failed to update stats for user {User}", message.Username);
        }
    }

    private static string TruncateForLog(string value, int maxLength = 60)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, maxLength), "…");
    }
}
