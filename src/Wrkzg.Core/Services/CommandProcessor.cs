using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates — acceptable in application-level services
#pragma warning disable CA1873 // Avoid potentially expensive call

namespace Wrkzg.Core.Services;

/// <summary>
/// Processes incoming chat messages against registered custom commands.
///
/// Pipeline:
///   1. Check if message starts with '!' (command prefix)
///   2. Look up command by trigger or alias
///   3. Verify command is enabled
///   4. Check user permission level
///   5. Check global and per-user cooldowns
///   6. Resolve response template variables
///   7. Send response to chat
///   8. Update command use count
///   9. Set cooldowns
///
/// Cooldowns are tracked in-memory (ConcurrentDictionary) — no DB overhead.
/// This is a Singleton service that maintains cooldown state across messages.
/// Uses IServiceScopeFactory to resolve Scoped repositories (ICommandRepository, IUserRepository).
/// </summary>
public class CommandProcessor : ICommandProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chat;
    private readonly IReadOnlyList<ISystemCommand> _systemCommands;
    private readonly ILogger<CommandProcessor> _logger;

    /// <summary>Global cooldown per command trigger. Key: trigger, Value: expiry time.</summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> _globalCooldowns = new();

    /// <summary>Per-user cooldown. Key: "trigger:twitchId", Value: expiry time.</summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> _userCooldowns = new();

    /// <summary>Regex for {random:min:max} template variable.</summary>
    private static readonly Regex RandomPattern = new(
        @"\{random:(\d+):(\d+)\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CommandProcessor(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chat,
        IEnumerable<ISystemCommand> systemCommands,
        ILogger<CommandProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _chat = chat;
        _systemCommands = systemCommands.ToList();
        _logger = logger;
    }

    public async Task<bool> HandleMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        // 1. Must start with command prefix
        if (string.IsNullOrEmpty(message.Content) || !message.Content.StartsWith('!'))
        {
            return false;
        }

        // 2. Extract trigger (first word, lowercase)
        string trigger = message.Content.Split(' ', 2)[0].ToLowerInvariant();

        // 2.5 Check system commands first (before DB lookup)
        foreach (ISystemCommand sysCmd in _systemCommands)
        {
            if (string.Equals(sysCmd.Trigger, trigger, StringComparison.OrdinalIgnoreCase)
                || sysCmd.Aliases.Any(a => string.Equals(a, trigger, StringComparison.OrdinalIgnoreCase)))
            {
                string? sysResponse = await sysCmd.ExecuteAsync(message, ct);
                if (sysResponse is not null)
                {
                    await _chat.SendMessageAsync(sysResponse, ct);
                    _logger.LogInformation("Executed system command {Trigger} for {User}",
                        sysCmd.Trigger, message.DisplayName);
                }

                return true;
            }
        }

        // Resolve scoped repositories
        using IServiceScope scope = _scopeFactory.CreateScope();
        ICommandRepository commands = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // 3. Look up command
        Command? command = await commands.GetByTriggerOrAliasAsync(trigger, ct);

        if (command is null || !command.IsEnabled)
        {
            return false;
        }

        // 4. Get or create user
        User user = await users.GetOrCreateAsync(message.UserId, message.Username, ct);

        // 5. Check permission
        if (!HasPermission(message, user, command.PermissionLevel))
        {
            _logger.LogDebug(
                "User {User} lacks permission {Required} for command {Trigger}",
                message.Username, command.PermissionLevel, command.Trigger);
            return false;
        }

        // 6. Check cooldowns
        if (IsOnCooldown(command, user))
        {
            _logger.LogDebug(
                "Command {Trigger} is on cooldown for user {User}",
                command.Trigger, message.Username);
            return false;
        }

        // 7. Resolve template
        string response = ResolveTemplate(command.ResponseTemplate, message, user);

        // 8. Send response
        await _chat.SendMessageAsync(response, ct);

        _logger.LogInformation(
            "Executed command {Trigger} for {User} → {Response}",
            command.Trigger, message.DisplayName, TruncateForLog(response));

        // 9. Update stats
        command.UseCount++;
        await commands.UpdateAsync(command, ct);

        // 10. Set cooldowns
        SetCooldowns(command, user);

        return true;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Permission Check
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if the user has sufficient permission to execute the command.
    /// </summary>
    private static bool HasPermission(ChatMessage message, User user, PermissionLevel required)
    {
        PermissionLevel userLevel = DeterminePermissionLevel(message, user);
        return userLevel >= required;
    }

    /// <summary>
    /// Determines the user's effective permission level from their Twitch state.
    /// Broadcaster > Moderator > Subscriber > Follower > Everyone.
    /// </summary>
    private static PermissionLevel DeterminePermissionLevel(ChatMessage message, User user)
    {
        if (message.IsBroadcaster)
        {
            return PermissionLevel.Broadcaster;
        }

        if (message.IsModerator || user.IsMod)
        {
            return PermissionLevel.Moderator;
        }

        if (message.IsSubscriber || user.IsSubscriber)
        {
            return PermissionLevel.Subscriber;
        }

        if (user.FollowDate.HasValue)
        {
            return PermissionLevel.Follower;
        }

        return PermissionLevel.Everyone;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Cooldown Check
    // ═══════════════════════════════════════════════════════════════════

    private bool IsOnCooldown(Command command, User user)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Global cooldown
        if (command.GlobalCooldownSeconds > 0)
        {
            if (_globalCooldowns.TryGetValue(command.Trigger, out DateTimeOffset globalExpiry)
                && now < globalExpiry)
            {
                return true;
            }
        }

        // Per-user cooldown
        if (command.UserCooldownSeconds > 0)
        {
            string userKey = $"{command.Trigger}:{user.TwitchId}";
            if (_userCooldowns.TryGetValue(userKey, out DateTimeOffset userExpiry)
                && now < userExpiry)
            {
                return true;
            }
        }

        return false;
    }

    private void SetCooldowns(Command command, User user)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (command.GlobalCooldownSeconds > 0)
        {
            _globalCooldowns[command.Trigger] = now.AddSeconds(command.GlobalCooldownSeconds);
        }

        if (command.UserCooldownSeconds > 0)
        {
            string userKey = $"{command.Trigger}:{user.TwitchId}";
            _userCooldowns[userKey] = now.AddSeconds(command.UserCooldownSeconds);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Template Resolution
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves template variables in the command response:
    ///   {user}           → display name of the invoking user
    ///   {count}          → how many times this command has been used
    ///   {points}         → user's current point balance
    ///   {watchtime}      → user's watched time in hours and minutes
    ///   {random:min:max} → random integer between min and max (inclusive)
    /// </summary>
    private static string ResolveTemplate(string template, ChatMessage message, User user)
    {
        string result = template
            .Replace("{user}", message.DisplayName, StringComparison.OrdinalIgnoreCase)
            .Replace("{count}", user.MessageCount.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{points}", user.Points.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{watchtime}", FormatWatchTime(user.WatchedMinutes), StringComparison.OrdinalIgnoreCase);

        // {random:min:max}
        result = RandomPattern.Replace(result, match =>
        {
            if (int.TryParse(match.Groups[1].Value, out int min)
                && int.TryParse(match.Groups[2].Value, out int max)
                && max >= min)
            {
                return Random.Shared.Next(min, max + 1).ToString(CultureInfo.InvariantCulture);
            }

            return match.Value; // Leave unresolved if invalid
        });

        // TODO: {uptime} — requires stream status from Helix API (future step)

        return result;
    }

    /// <summary>
    /// Formats watched minutes as "Xh Ym" (e.g. "2h 14m").
    /// </summary>
    private static string FormatWatchTime(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }

        return $"{minutes}m";
    }

    /// <summary>
    /// Truncates a string for structured logging.
    /// </summary>
    private static string TruncateForLog(string value, int maxLength = 80)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, maxLength), "…");
    }
}
