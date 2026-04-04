using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Effects.Triggers;

/// <summary>Triggers when a specific chat command is used.</summary>
public class CommandTrigger : ITriggerType
{
    /// <inheritdoc />
    public string Id => "command";

    /// <inheritdoc />
    public string DisplayName => "Chat Command";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "trigger" };

    /// <summary>Returns <c>true</c> when the first word of the chat message matches the configured trigger.</summary>
    public Task<bool> MatchesAsync(EffectTriggerContext context, CancellationToken ct = default)
    {
        if (!string.Equals(context.EventType, "chat_message", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        string trigger = context.GetData("trigger").ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(context.MessageContent))
        {
            return Task.FromResult(false);
        }

        string firstWord = context.MessageContent.Split(' ', 2)[0].ToLowerInvariant();
        return Task.FromResult(string.Equals(firstWord, trigger, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Triggers on specific Twitch events (follow, subscribe, raid, etc.).</summary>
public class EventTrigger : ITriggerType
{
    /// <inheritdoc />
    public string Id => "event";

    /// <inheritdoc />
    public string DisplayName => "Twitch Event";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "event_type" };

    /// <summary>Returns <c>true</c> when the event type matches the configured <c>event_type</c> parameter.</summary>
    public Task<bool> MatchesAsync(EffectTriggerContext context, CancellationToken ct = default)
    {
        string requiredEvent = context.GetData("event_type");
        bool matches = string.Equals(context.EventType, requiredEvent, StringComparison.OrdinalIgnoreCase)
            || context.EventType.StartsWith("event.", StringComparison.OrdinalIgnoreCase)
               && string.Equals(context.EventType, $"event.{requiredEvent}", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(matches);
    }
}

/// <summary>Triggers when a specific keyword appears in chat (not as a command).</summary>
public class KeywordTrigger : ITriggerType
{
    /// <inheritdoc />
    public string Id => "keyword";

    /// <inheritdoc />
    public string DisplayName => "Chat Keyword";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "keyword" };

    /// <summary>Returns <c>true</c> when the chat message contains the configured keyword (case-insensitive).</summary>
    public Task<bool> MatchesAsync(EffectTriggerContext context, CancellationToken ct = default)
    {
        if (!string.Equals(context.EventType, "chat_message", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        string keyword = context.GetData("keyword");
        if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(context.MessageContent))
        {
            return Task.FromResult(false);
        }

        bool contains = context.MessageContent.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(contains);
    }
}

/// <summary>Triggers when a hotkey is pressed.</summary>
public class HotkeyTrigger : ITriggerType
{
    /// <inheritdoc />
    public string Id => "hotkey";

    /// <inheritdoc />
    public string DisplayName => "Hotkey Press";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "hotkey_id" };

    /// <summary>Returns <c>true</c> when the hotkey binding ID matches the configured <c>hotkey_id</c>.</summary>
    public Task<bool> MatchesAsync(EffectTriggerContext context, CancellationToken ct = default)
    {
        if (!string.Equals(context.EventType, "hotkey", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        string requiredId = context.GetData("hotkey_id");
        string actualId = context.GetData("binding_id");
        return Task.FromResult(string.Equals(requiredId, actualId, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Triggers on channel point redemptions.</summary>
public class ChannelPointTrigger : ITriggerType
{
    /// <inheritdoc />
    public string Id => "channelpoint";

    /// <inheritdoc />
    public string DisplayName => "Channel Point Redemption";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "reward_id" };

    /// <summary>
    /// Returns <c>true</c> when the event is a channel point redemption matching the configured reward ID.
    /// If no reward ID is configured, matches any redemption.
    /// </summary>
    public Task<bool> MatchesAsync(EffectTriggerContext context, CancellationToken ct = default)
    {
        if (!string.Equals(context.EventType, "channelpoint", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        string requiredReward = context.GetData("reward_id");
        if (string.IsNullOrWhiteSpace(requiredReward))
        {
            return Task.FromResult(true); // Match any redemption
        }

        string actualReward = context.GetData("reward_id_actual");
        return Task.FromResult(string.Equals(requiredReward, actualReward, StringComparison.OrdinalIgnoreCase));
    }
}
