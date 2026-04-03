using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Effects.Triggers;

/// <summary>Triggers when a specific chat command is used.</summary>
public class CommandTrigger : ITriggerType
{
    public string Id => "command";
    public string DisplayName => "Chat Command";
    public string[] ParameterKeys => new[] { "trigger" };

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
    public string Id => "event";
    public string DisplayName => "Twitch Event";
    public string[] ParameterKeys => new[] { "event_type" };

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
    public string Id => "keyword";
    public string DisplayName => "Chat Keyword";
    public string[] ParameterKeys => new[] { "keyword" };

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
    public string Id => "hotkey";
    public string DisplayName => "Hotkey Press";
    public string[] ParameterKeys => new[] { "hotkey_id" };

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
    public string Id => "channelpoint";
    public string DisplayName => "Channel Point Redemption";
    public string[] ParameterKeys => new[] { "reward_id" };

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
