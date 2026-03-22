using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848, CA1873 // Use LoggerMessage delegates — acceptable in application-level services

namespace Wrkzg.Core.Services;

/// <summary>
/// Checks incoming chat messages for spam violations.
/// Integrated into ChatMessagePipeline BEFORE command processing.
/// </summary>
public class SpamFilterService
{
    private readonly ISettingsRepository _settings;
    private readonly ITwitchChatClient _chat;
    private readonly ITwitchHelixClient _helix;
    private readonly ILogger<SpamFilterService> _logger;

    private readonly ConcurrentDictionary<string, (string LastMessage, int Count)> _recentMessages = new();

    private static readonly Regex UrlPattern = new(
        @"(https?://|www\.)\S+|[a-zA-Z0-9][-a-zA-Z0-9]*\.(com|net|org|tv|gg|me|io|co|de|uk|fr|ru)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SpamFilterService(
        ISettingsRepository settings,
        ITwitchChatClient chat,
        ITwitchHelixClient helix,
        ILogger<SpamFilterService> logger)
    {
        _settings = settings;
        _chat = chat;
        _helix = helix;
        _logger = logger;
    }

    /// <summary>
    /// Checks a message against all enabled spam filters.
    /// Returns true if the message was flagged as spam (and action was taken).
    /// </summary>
    public async Task<bool> CheckAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (message.IsBroadcaster)
        {
            return false;
        }

        SpamFilterConfig config = await LoadConfigAsync(ct);

        SpamViolation? violation =
            CheckLinks(message, config) ??
            CheckCaps(message, config) ??
            CheckBannedWords(message, config) ??
            CheckRepetition(message, config);

        if (violation is null)
        {
            return false;
        }

        await TakeActionAsync(message, violation, ct);
        return true;
    }

    private static SpamViolation? CheckLinks(ChatMessage message, SpamFilterConfig config)
    {
        if (!config.LinksEnabled)
        {
            return null;
        }
        if (message.IsModerator && config.LinksModsExempt)
        {
            return null;
        }
        if (message.IsSubscriber && config.LinksSubsExempt)
        {
            return null;
        }

        Match match = UrlPattern.Match(message.Content);
        if (!match.Success)
        {
            return null;
        }

        string[] whitelist = config.LinkWhitelist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string url = match.Value.ToLowerInvariant();
        if (whitelist.Any(w => url.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return new SpamViolation("Links", config.LinksTimeoutSeconds,
            $"@{message.Username}, links are not allowed in chat.");
    }

    private static SpamViolation? CheckCaps(ChatMessage message, SpamFilterConfig config)
    {
        if (!config.CapsEnabled)
        {
            return null;
        }
        if (message.IsModerator)
        {
            return null;
        }
        if (message.IsSubscriber && config.CapsSubsExempt)
        {
            return null;
        }
        if (message.Content.Length < config.CapsMinLength)
        {
            return null;
        }

        int upperCount = message.Content.Count(char.IsUpper);
        int letterCount = message.Content.Count(char.IsLetter);
        if (letterCount == 0)
        {
            return null;
        }

        double capsPercent = (double)upperCount / letterCount * 100;
        if (capsPercent <= config.CapsMaxPercent)
        {
            return null;
        }

        return new SpamViolation("Caps", config.CapsTimeoutSeconds,
            $"@{message.Username}, please don't use excessive caps.");
    }

    private static SpamViolation? CheckBannedWords(ChatMessage message, SpamFilterConfig config)
    {
        if (!config.BannedWordsEnabled)
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(config.BannedWordsList))
        {
            return null;
        }
        if (message.IsModerator)
        {
            return null;
        }
        if (message.IsSubscriber && config.BannedWordsSubsExempt)
        {
            return null;
        }

        string[] bannedWords = config.BannedWordsList
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        string contentLower = message.Content.ToLowerInvariant();
        foreach (string banned in bannedWords)
        {
            if (contentLower.Contains(banned.ToLowerInvariant(), StringComparison.Ordinal))
            {
                return new SpamViolation("BannedWord", config.BannedWordsTimeoutSeconds,
                    $"@{message.Username}, that word is not allowed.");
            }
        }

        return null;
    }

    private SpamViolation? CheckRepetition(ChatMessage message, SpamFilterConfig config)
    {
        if (!config.RepeatEnabled)
        {
            return null;
        }
        if (message.IsModerator)
        {
            return null;
        }
        if (message.IsSubscriber && config.RepeatSubsExempt)
        {
            return null;
        }

        string key = message.UserId;
        string content = message.Content.Trim().ToLowerInvariant();

        _recentMessages.AddOrUpdate(key,
            _ => (content, 1),
            (_, prev) =>
            {
                if (prev.LastMessage == content)
                {
                    return (content, prev.Count + 1);
                }
                return (content, 1);
            });

        if (_recentMessages.TryGetValue(key, out (string LastMessage, int Count) state) && state.Count > config.RepeatMaxCount)
        {
            _recentMessages[key] = (content, 0);
            return new SpamViolation("Repetition", config.RepeatTimeoutSeconds,
                $"@{message.Username}, please don't repeat the same message.");
        }

        return null;
    }

    private async Task TakeActionAsync(ChatMessage message, SpamViolation violation, CancellationToken ct)
    {
        _logger.LogInformation("Spam filter [{Filter}] triggered for {User}: {Message}",
            violation.FilterName, message.Username, message.Content.Length > 60 ? message.Content[..60] + "\u2026" : message.Content);

        try
        {
            await _chat.SendMessageAsync(violation.WarningMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send spam warning");
        }

        if (violation.TimeoutSeconds > 0)
        {
            try
            {
                await _helix.TimeoutUserAsync(message.UserId, violation.TimeoutSeconds, $"Spam filter: {violation.FilterName}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to timeout user {User} via Helix", message.Username);
            }
        }
    }

    // ─── Config Loading ──────────────────────────

    private async Task<SpamFilterConfig> LoadConfigAsync(CancellationToken ct)
    {
        SpamFilterConfig config = new();

        config.LinksEnabled = await GetBoolAsync("spam.links.enabled", config.LinksEnabled, ct);
        config.LinksTimeoutSeconds = await GetIntAsync("spam.links.timeout", config.LinksTimeoutSeconds, ct);
        config.LinksSubsExempt = await GetBoolAsync("spam.links.subs_exempt", config.LinksSubsExempt, ct);
        config.LinkWhitelist = await GetStringAsync("spam.links.whitelist", config.LinkWhitelist, ct);

        config.CapsEnabled = await GetBoolAsync("spam.caps.enabled", config.CapsEnabled, ct);
        config.CapsMinLength = await GetIntAsync("spam.caps.min_length", config.CapsMinLength, ct);
        config.CapsMaxPercent = await GetIntAsync("spam.caps.max_percent", config.CapsMaxPercent, ct);
        config.CapsTimeoutSeconds = await GetIntAsync("spam.caps.timeout", config.CapsTimeoutSeconds, ct);
        config.CapsSubsExempt = await GetBoolAsync("spam.caps.subs_exempt", config.CapsSubsExempt, ct);

        config.BannedWordsEnabled = await GetBoolAsync("spam.banned.enabled", config.BannedWordsEnabled, ct);
        config.BannedWordsList = await GetStringAsync("spam.banned.words", config.BannedWordsList, ct);
        config.BannedWordsTimeoutSeconds = await GetIntAsync("spam.banned.timeout", config.BannedWordsTimeoutSeconds, ct);
        config.BannedWordsSubsExempt = await GetBoolAsync("spam.banned.subs_exempt", config.BannedWordsSubsExempt, ct);

        config.RepeatEnabled = await GetBoolAsync("spam.repeat.enabled", config.RepeatEnabled, ct);
        config.RepeatMaxCount = await GetIntAsync("spam.repeat.max_count", config.RepeatMaxCount, ct);
        config.RepeatTimeoutSeconds = await GetIntAsync("spam.repeat.timeout", config.RepeatTimeoutSeconds, ct);
        config.RepeatSubsExempt = await GetBoolAsync("spam.repeat.subs_exempt", config.RepeatSubsExempt, ct);

        return config;
    }

    private async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct)
    {
        string? val = await _settings.GetAsync(key, ct);
        return val is not null ? bool.TryParse(val, out bool result) && result : defaultValue;
    }

    private async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct)
    {
        string? val = await _settings.GetAsync(key, ct);
        return val is not null && int.TryParse(val, out int result) ? result : defaultValue;
    }

    private async Task<string> GetStringAsync(string key, string defaultValue, CancellationToken ct)
    {
        string? val = await _settings.GetAsync(key, ct);
        return val ?? defaultValue;
    }
}

public record SpamViolation(string FilterName, int TimeoutSeconds, string WarningMessage);
