using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Effects.EffectTypes;

/// <summary>Sends a chat message.</summary>
public class ChatMessageEffect : IEffectType
{
    /// <inheritdoc />
    public string Id => "chat_message";

    /// <inheritdoc />
    public string DisplayName => "Send Chat Message";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "message" };

    /// <summary>Resolves template variables in the <c>message</c> parameter and sends it to chat.</summary>
    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        string template = context.GetParameter("message");
        string message = context.ResolveVariables(template);

        if (context.Scope is not null && !string.IsNullOrWhiteSpace(message))
        {
            ITwitchChatClient chat = context.Scope.ServiceProvider.GetRequiredService<ITwitchChatClient>();
            if (chat.IsConnected)
            {
                await chat.SendMessageAsync(message, ct);
            }
        }
    }
}

/// <summary>Waits for a specified duration (used between effects in a chain).</summary>
public class WaitEffect : IEffectType
{
    /// <inheritdoc />
    public string Id => "wait";

    /// <inheritdoc />
    public string DisplayName => "Wait";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "seconds" };

    /// <summary>Delays execution for the configured number of seconds, clamped to a maximum of 60.</summary>
    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        if (int.TryParse(context.GetParameter("seconds"), out int seconds) && seconds > 0)
        {
            int clampedMs = Math.Min(seconds, 60) * 1000;
            await Task.Delay(clampedMs, ct);
        }
    }
}

/// <summary>Modifies a counter value.</summary>
public class CounterEffect : IEffectType
{
    /// <inheritdoc />
    public string Id => "counter";

    /// <inheritdoc />
    public string DisplayName => "Update Counter";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "counter_id", "action" };

    /// <summary>
    /// Looks up a counter by ID and applies the configured action (increment, decrement, or reset),
    /// then broadcasts the update via SignalR.
    /// </summary>
    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        if (context.Scope is null)
        {
            return;
        }

        if (!int.TryParse(context.GetParameter("counter_id"), out int counterId))
        {
            return;
        }

        string action = context.GetParameter("action");

        ICounterRepository counters = context.Scope.ServiceProvider.GetRequiredService<ICounterRepository>();
        Counter? counter = await counters.GetByIdAsync(counterId, ct);
        if (counter is null)
        {
            return;
        }

        switch (action.ToLowerInvariant())
        {
            case "increment": case "+1": counter.Value++; break;
            case "decrement": case "-1": counter.Value--; break;
            case "reset": case "0": counter.Value = 0; break;
        }

        await counters.UpdateAsync(counter, ct);

        IChatEventBroadcaster broadcaster = context.Scope.ServiceProvider.GetRequiredService<IChatEventBroadcaster>();
        await broadcaster.BroadcastCounterUpdatedAsync(counter.Id, counter.Name, counter.Value, ct);
    }
}

/// <summary>Sends an alert to the overlay via SignalR.</summary>
public class AlertEffect : IEffectType
{
    /// <inheritdoc />
    public string Id => "alert";

    /// <inheritdoc />
    public string DisplayName => "Show Alert";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "message" };

    /// <summary>Resolves the message template and broadcasts an alert event to all connected overlays.</summary>
    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        if (context.Scope is null)
        {
            return;
        }

        string message = context.ResolveVariables(context.GetParameter("message"));

        IChatEventBroadcaster broadcaster = context.Scope.ServiceProvider.GetRequiredService<IChatEventBroadcaster>();
        await broadcaster.BroadcastFollowEventAsync(message, ct);
    }
}

/// <summary>Sets a shared variable for use in subsequent effects.</summary>
public class VariableEffect : IEffectType
{
    /// <inheritdoc />
    public string Id => "variable";

    /// <inheritdoc />
    public string DisplayName => "Set Variable";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "name", "value" };

    /// <summary>
    /// Stores a named variable in the shared execution context so later effects in the chain
    /// can reference it via template substitution.
    /// </summary>
    public Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        string name = context.GetParameter("name");
        string value = context.ResolveVariables(context.GetParameter("value"));

        if (!string.IsNullOrWhiteSpace(name))
        {
            context.Variables[name] = value;
        }

        return Task.CompletedTask;
    }
}
