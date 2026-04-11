using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Effects.Conditions;

/// <summary>Checks if the user has a minimum role priority.</summary>
public class RoleCheckCondition : IConditionType
{
    /// <inheritdoc />
    public string Id => "role_check";

    /// <inheritdoc />
    public string DisplayName => "Role Check";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "min_priority" };

    /// <summary>
    /// Evaluates whether the triggering user's highest role priority meets the configured minimum.
    /// Returns <c>true</c> when the parameter is missing or invalid, allowing the effect to proceed.
    /// </summary>
    public async Task<bool> EvaluateAsync(EffectConditionContext context, CancellationToken ct = default)
    {
        if (!int.TryParse(context.GetParameter("min_priority"), out int minPriority) || minPriority <= 0)
        {
            return true;
        }

        if (context.Scope is null || string.IsNullOrWhiteSpace(context.Trigger.UserId))
        {
            return false;
        }

        IUserRepository users = context.Scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User? user = await users.GetByTwitchIdAsync(context.Trigger.UserId, ct);
        if (user is null)
        {
            return false;
        }

        IRoleRepository roles = context.Scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        int userPriority = await roles.GetHighestPriorityForUserAsync(user.Id, ct);
        return userPriority >= minPriority;
    }
}

/// <summary>Checks if the user has enough points.</summary>
public class PointsCheckCondition : IConditionType
{
    /// <inheritdoc />
    public string Id => "points_check";

    /// <inheritdoc />
    public string DisplayName => "Points Check";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "min_points" };

    /// <summary>
    /// Evaluates whether the triggering user has at least the configured minimum point balance.
    /// Returns <c>true</c> when the parameter is missing or unparseable.
    /// </summary>
    public async Task<bool> EvaluateAsync(EffectConditionContext context, CancellationToken ct = default)
    {
        if (!long.TryParse(context.GetParameter("min_points"), out long minPoints))
        {
            return true;
        }

        if (context.Scope is null || string.IsNullOrWhiteSpace(context.Trigger.UserId))
        {
            return false;
        }

        IUserRepository users = context.Scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User? user = await users.GetByTwitchIdAsync(context.Trigger.UserId, ct);
        return user is not null && user.Points >= minPoints;
    }
}

/// <summary>Random chance gate -- passes with a given probability.</summary>
public class RandomChanceCondition : IConditionType
{
    /// <inheritdoc />
    public string Id => "random_chance";

    /// <inheritdoc />
    public string DisplayName => "Random Chance";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "percent" };

    /// <summary>
    /// Rolls a random number 0-99 and passes when it falls below the configured percent threshold.
    /// Returns <c>true</c> when the parameter is missing or unparseable.
    /// </summary>
    public Task<bool> EvaluateAsync(EffectConditionContext context, CancellationToken ct = default)
    {
        if (!int.TryParse(context.GetParameter("percent"), out int percent))
        {
            return Task.FromResult(true);
        }

        int roll = Random.Shared.Next(100);
        return Task.FromResult(roll < percent);
    }
}

/// <summary>Checks if the stream is currently live or offline.</summary>
public class StreamStatusCondition : IConditionType
{
    /// <inheritdoc />
    public string Id => "stream_status";

    /// <inheritdoc />
    public string DisplayName => "Stream Status";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "require_live" };

    /// <summary>
    /// Queries the Twitch Helix API to determine whether the channel is live,
    /// then compares against the <c>require_live</c> parameter.
    /// </summary>
    public async Task<bool> EvaluateAsync(EffectConditionContext context, CancellationToken ct = default)
    {
        if (context.Scope is null)
        {
            return true;
        }

        bool requireLive = string.Equals(context.GetParameter("require_live"), "true", StringComparison.OrdinalIgnoreCase);

        ISettingsRepository settings = context.Scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        string? channel = await settings.GetAsync("Bot.Channel", ct);
        if (string.IsNullOrWhiteSpace(channel))
        {
            return !requireLive;
        }

        IBroadcasterHelixClient helix = context.Scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
        StreamInfo? stream = await helix.GetStreamAsync(channel, ct);
        bool isLive = stream is not null;
        return requireLive ? isLive : !isLive;
    }
}
