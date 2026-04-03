using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Effects;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Processes every incoming chat message through the full pipeline.
///
/// Called by BotConnectionService for each message received from IRC.
/// Runs in order:
///   1. Update user stats (message count, last seen, role sync)
///   2. Mark user active (watch time tracking)
///   3. Increment chat line counter (timed messages)
///   4. Try raffle keyword entry
///   5. Spam filter (links, caps, banned words, repetition)
///   6. Counter commands (!trigger, !trigger+, !trigger-)
///   7. CommandProcessor (system commands + custom commands)
///
/// Uses IServiceScopeFactory to resolve Scoped dependencies (repositories)
/// because this service is Singleton (registered via BotConnectionService).
/// </summary>
public class ChatMessagePipeline
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IUserTrackingService _tracking;
    private readonly TimedMessageService _timedMessageService;
    private readonly ChatGameManager _chatGameManager;
    private readonly EffectEngine _effectEngine;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatMessagePipeline> _logger;

    public ChatMessagePipeline(
        ICommandProcessor commandProcessor,
        IUserTrackingService tracking,
        TimedMessageService timedMessageService,
        ChatGameManager chatGameManager,
        EffectEngine effectEngine,
        IChatEventBroadcaster broadcaster,
        IServiceScopeFactory scopeFactory,
        ILogger<ChatMessagePipeline> logger)
    {
        _commandProcessor = commandProcessor;
        _tracking = tracking;
        _timedMessageService = timedMessageService;
        _chatGameManager = chatGameManager;
        _effectEngine = effectEngine;
        _broadcaster = broadcaster;
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

            // 2. Increment chat line counter for timed messages
            _timedMessageService.IncrementChatLineCounter();

            // 3. Check for raffle keyword entry (before command processing)
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

            // 5. Spam filter — BEFORE commands
            try
            {
                using IServiceScope spamScope = _scopeFactory.CreateScope();
                SpamFilterService spamFilter = spamScope.ServiceProvider.GetRequiredService<SpamFilterService>();
                bool isSpam = await spamFilter.CheckAsync(message, ct);
                if (isSpam)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Spam filter error for message from {User}", message.Username);
            }

            // 6. Counter commands (dynamic — !trigger shows, !trigger+ increments, !trigger- decrements)
            if (message.Content.StartsWith('!'))
            {
                try
                {
                    using IServiceScope counterScope = _scopeFactory.CreateScope();
                    ICounterRepository counters = counterScope.ServiceProvider.GetRequiredService<ICounterRepository>();

                    string content = message.Content.Trim();
                    string command = content.Split(' ', 2)[0][1..].ToLowerInvariant();
                    bool isIncrement = command.EndsWith('+');
                    bool isDecrement = command.EndsWith('-');
                    string triggerName = isIncrement || isDecrement ? command[..^1] : command;
                    string trigger = "!" + triggerName;

                    Counter? counter = await counters.GetByTriggerAsync(trigger, ct);
                    if (counter is not null)
                    {
                        bool changed = false;
                        if (isIncrement && (message.IsModerator || message.IsBroadcaster))
                        {
                            counter.Value++;
                            await counters.UpdateAsync(counter, ct);
                            changed = true;
                        }
                        else if (isDecrement && (message.IsModerator || message.IsBroadcaster))
                        {
                            counter.Value--;
                            await counters.UpdateAsync(counter, ct);
                            changed = true;
                        }

                        if (changed)
                        {
                            await _broadcaster.BroadcastCounterUpdatedAsync(counter.Id, counter.Name, counter.Value, ct);
                        }

                        string response = counter.ResponseTemplate
                            .Replace("{name}", counter.Name)
                            .Replace("{value}", counter.Value.ToString(CultureInfo.InvariantCulture));

                        ITwitchChatClient chat = counterScope.ServiceProvider.GetRequiredService<ITwitchChatClient>();
                        if (chat.IsConnected)
                        {
                            await chat.SendMessageAsync(response, ct);
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Counter command error for message from {User}", message.Username);
                }
            }

            // 7. Try custom commands
            bool handled = await _commandProcessor.HandleMessageAsync(message, ct);

            if (handled)
            {
                return;
            }

            // 8. Check active game rounds (e.g. trivia answers, duel !accept)
            bool gameRoundHandled = await _chatGameManager.HandleActiveRoundMessageAsync(message, ct);
            if (gameRoundHandled)
            {
                return;
            }

            // 9. Check game triggers (e.g. !heist 100, !slots 50)
            bool gameHandled = await _chatGameManager.HandleMessageAsync(message, ct);
            if (gameHandled)
            {
                return;
            }

            // 10. Effect Engine — evaluate all EffectLists against this chat message
            try
            {
                EffectTriggerContext effectContext = new()
                {
                    EventType = "chat_message",
                    UserId = message.UserId,
                    Username = message.DisplayName,
                    MessageContent = message.Content,
                    Data = new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["channel"] = message.Channel ?? "",
                        ["isMod"] = message.IsModerator.ToString(),
                        ["isSub"] = message.IsSubscriber.ToString(),
                        ["isBroadcaster"] = message.IsBroadcaster.ToString(),
                    }
                };
                await _effectEngine.ProcessAsync(effectContext, ct);
            }
            catch (Exception effectEx)
            {
                _logger.LogWarning(effectEx, "Effect engine error for message from {User}", message.Username);
            }
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
