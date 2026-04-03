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
    public string Id => "chat_message";
    public string DisplayName => "Send Chat Message";
    public string[] ParameterKeys => new[] { "message" };

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
    public string Id => "wait";
    public string DisplayName => "Wait";
    public string[] ParameterKeys => new[] { "seconds" };

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
    public string Id => "counter";
    public string DisplayName => "Update Counter";
    public string[] ParameterKeys => new[] { "counter_id", "action" };

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
    public string Id => "alert";
    public string DisplayName => "Show Alert";
    public string[] ParameterKeys => new[] { "message" };

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
    public string Id => "variable";
    public string DisplayName => "Set Variable";
    public string[] ParameterKeys => new[] { "name", "value" };

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
