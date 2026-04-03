using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Wrkzg.Core.Effects;

/// <summary>
/// Context passed to trigger matching. Contains the event data.
/// </summary>
public record EffectTriggerContext
{
    /// <summary>Type of event that occurred (e.g. "chat_message", "event.follow", "hotkey").</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>User who triggered the event (TwitchId or display name).</summary>
    public string? UserId { get; init; }

    /// <summary>Username/display name.</summary>
    public string? Username { get; init; }

    /// <summary>Message content (for chat triggers).</summary>
    public string? MessageContent { get; init; }

    /// <summary>Additional event-specific data.</summary>
    public Dictionary<string, string> Data { get; init; } = new();

    /// <summary>Service scope for DB access.</summary>
    public IServiceScope? Scope { get; init; }

    /// <summary>Gets a data value by key.</summary>
    public string GetData(string key) => Data.GetValueOrDefault(key) ?? string.Empty;
}

/// <summary>
/// Context passed to condition evaluation.
/// </summary>
public class EffectConditionContext
{
    /// <summary>The trigger context that started this evaluation.</summary>
    public EffectTriggerContext Trigger { get; init; } = null!;

    /// <summary>Condition parameters from the EffectList JSON config.</summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>Service scope for DB access.</summary>
    public IServiceScope? Scope { get; init; }

    /// <summary>Gets a parameter value by key.</summary>
    public string GetParameter(string key) => Parameters.GetValueOrDefault(key) ?? string.Empty;
}

/// <summary>
/// Context passed to effect execution.
/// </summary>
public class EffectExecutionContext
{
    /// <summary>The trigger context that started this execution.</summary>
    public EffectTriggerContext Trigger { get; init; } = null!;

    /// <summary>Effect parameters from the EffectList JSON config.</summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>Shared variables across effects in the same chain.</summary>
    public Dictionary<string, string> Variables { get; init; } = new();

    /// <summary>Service scope for DB access.</summary>
    public IServiceScope? Scope { get; init; }

    /// <summary>Gets a parameter value by key.</summary>
    public string GetParameter(string key) => Parameters.GetValueOrDefault(key) ?? string.Empty;

    /// <summary>Resolves template variables in a string ({user}, {variable_name}, etc.).</summary>
    public string ResolveVariables(string template)
    {
        string result = template;

        // Resolve trigger data
        if (Trigger.Username is not null)
        {
            result = result.Replace("{user}", Trigger.Username);
        }

        foreach (KeyValuePair<string, string> kvp in Trigger.Data)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }

        // Resolve shared variables
        foreach (KeyValuePair<string, string> kvp in Variables)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }

        return result;
    }
}
