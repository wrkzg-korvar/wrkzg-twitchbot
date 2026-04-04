using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Effects;

/// <summary>
/// The Effect Engine — evaluates all registered EffectLists against incoming events.
/// Called from ChatMessagePipeline, EventSubConnectionService, HotkeyListenerService, etc.
/// Singleton service that caches enabled EffectLists and manages cooldowns.
/// </summary>
public class EffectEngine
{
    private readonly IReadOnlyList<ITriggerType> _triggerTypes;
    private readonly IReadOnlyList<IConditionType> _conditionTypes;
    private readonly IReadOnlyList<IEffectType> _effectTypes;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EffectEngine> _logger;

    private readonly ConcurrentDictionary<int, DateTimeOffset> _cooldowns = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectEngine"/> class with the registered
    /// trigger, condition, and effect type implementations.
    /// </summary>
    /// <param name="triggerTypes">All registered trigger type implementations.</param>
    /// <param name="conditionTypes">All registered condition type implementations.</param>
    /// <param name="effectTypes">All registered effect type implementations.</param>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public EffectEngine(
        IEnumerable<ITriggerType> triggerTypes,
        IEnumerable<IConditionType> conditionTypes,
        IEnumerable<IEffectType> effectTypes,
        IServiceScopeFactory scopeFactory,
        ILogger<EffectEngine> logger)
    {
        _triggerTypes = triggerTypes.ToList();
        _conditionTypes = conditionTypes.ToList();
        _effectTypes = effectTypes.ToList();
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Returns all registered trigger types (for the dashboard dropdown).</summary>
    public IReadOnlyList<ITriggerType> GetTriggerTypes() => _triggerTypes;

    /// <summary>Returns all registered condition types.</summary>
    public IReadOnlyList<IConditionType> GetConditionTypes() => _conditionTypes;

    /// <summary>Returns all registered effect types.</summary>
    public IReadOnlyList<IEffectType> GetEffectTypes() => _effectTypes;

    /// <summary>
    /// Processes an event context against all enabled EffectLists.
    /// Called from pipeline components when an event occurs.
    /// </summary>
    public async Task ProcessAsync(EffectTriggerContext context, CancellationToken ct = default)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IEffectListRepository repo = scope.ServiceProvider.GetRequiredService<IEffectListRepository>();
            IReadOnlyList<EffectList> effectLists = await repo.GetEnabledAsync(ct);

            context = context with { Scope = scope };

            foreach (EffectList effectList in effectLists)
            {
                try
                {
                    await EvaluateEffectListAsync(effectList, context, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error evaluating effect list {Name} (ID: {Id})",
                        effectList.Name, effectList.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EffectEngine.ProcessAsync failed for event type {EventType}",
                context.EventType);
        }
    }

    private async Task EvaluateEffectListAsync(EffectList effectList, EffectTriggerContext context, CancellationToken ct)
    {
        // 1. Check cooldown
        if (effectList.Cooldown > 0 &&
            _cooldowns.TryGetValue(effectList.Id, out DateTimeOffset lastRun) &&
            (DateTimeOffset.UtcNow - lastRun).TotalSeconds < effectList.Cooldown)
        {
            return;
        }

        // 2. Match trigger
        ITriggerType? triggerType = _triggerTypes.FirstOrDefault(t =>
            string.Equals(t.Id, effectList.TriggerTypeId, StringComparison.OrdinalIgnoreCase));

        if (triggerType is null)
        {
            return;
        }

        // Parse trigger config and inject into context data
        Dictionary<string, string> triggerParams = ParseJsonConfig(effectList.TriggerConfig);
        EffectTriggerContext enrichedContext = context with
        {
            Data = MergeDictionaries(context.Data, triggerParams)
        };

        if (!await triggerType.MatchesAsync(enrichedContext, ct))
        {
            return;
        }

        // 3. Evaluate conditions (AND logic — all must pass)
        List<ConditionConfig> conditions = ParseConditions(effectList.ConditionsConfig);
        foreach (ConditionConfig condition in conditions)
        {
            IConditionType? conditionType = _conditionTypes.FirstOrDefault(c =>
                string.Equals(c.Id, condition.Type, StringComparison.OrdinalIgnoreCase));

            if (conditionType is null)
            {
                continue;
            }

            EffectConditionContext condContext = new()
            {
                Trigger = enrichedContext,
                Parameters = condition.Params,
                Scope = context.Scope
            };

            if (!await conditionType.EvaluateAsync(condContext, ct))
            {
                return; // Condition failed — stop
            }
        }

        // 4. Execute effect chain (sequential)
        _logger.LogInformation("Executing effect list: {Name}", effectList.Name);
        _cooldowns[effectList.Id] = DateTimeOffset.UtcNow;

        List<EffectConfig> effects = ParseEffects(effectList.EffectsConfig);
        Dictionary<string, string> sharedVariables = new();

        foreach (EffectConfig effect in effects)
        {
            IEffectType? effectType = _effectTypes.FirstOrDefault(e =>
                string.Equals(e.Id, effect.Type, StringComparison.OrdinalIgnoreCase));

            if (effectType is null)
            {
                _logger.LogWarning("Unknown effect type: {Type}", effect.Type);
                continue;
            }

            EffectExecutionContext execContext = new()
            {
                Trigger = enrichedContext,
                Parameters = effect.Params,
                Variables = sharedVariables,
                Scope = context.Scope
            };

            await effectType.ExecuteAsync(execContext, ct);
        }
    }

    private static Dictionary<string, string> ParseJsonConfig(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static List<ConditionConfig> ParseConditions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ConditionConfig>>(json, _jsonOptions)
                ?? new List<ConditionConfig>();
        }
        catch
        {
            return new List<ConditionConfig>();
        }
    }

    private static List<EffectConfig> ParseEffects(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<EffectConfig>>(json, _jsonOptions)
                ?? new List<EffectConfig>();
        }
        catch
        {
            return new List<EffectConfig>();
        }
    }

    private static Dictionary<string, string> MergeDictionaries(
        Dictionary<string, string> a, Dictionary<string, string> b)
    {
        Dictionary<string, string> result = new(a);
        foreach (KeyValuePair<string, string> kvp in b)
        {
            result.TryAdd(kvp.Key, kvp.Value);
        }
        return result;
    }
}

/// <summary>JSON structure for a condition in the ConditionsConfig array.</summary>
public class ConditionConfig
{
    /// <summary>Gets or sets the condition type identifier (e.g. "role_check", "points_check").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the key-value parameters for this condition.</summary>
    public Dictionary<string, string> Params { get; set; } = new();
}

/// <summary>JSON structure for an effect in the EffectsConfig array.</summary>
public class EffectConfig
{
    /// <summary>Gets or sets the effect type identifier (e.g. "chat_message", "alert").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the key-value parameters for this effect.</summary>
    public Dictionary<string, string> Params { get; set; } = new();
}
