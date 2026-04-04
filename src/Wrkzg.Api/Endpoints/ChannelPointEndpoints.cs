using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for Channel Point Reward handlers.
/// </summary>
public static class ChannelPointEndpoints
{
    /// <summary>Registers channel point reward handler API endpoints.</summary>
    public static void MapChannelPointEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/channel-points").WithTags("ChannelPoints");

        group.MapGet("/", async (IChannelPointRewardRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<ChannelPointReward> rewards = await repo.GetAllAsync(ct);
            return Results.Ok(rewards);
        });

        group.MapPost("/", async (CreateChannelPointHandlerRequest request, IChannelPointRewardRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TwitchRewardId))
            {
                return Results.BadRequest(new { error = "Twitch Reward ID is required." });
            }

            ChannelPointReward? existing = await repo.GetByTwitchRewardIdAsync(request.TwitchRewardId, ct);
            if (existing is not null)
            {
                return Results.BadRequest(new { error = "A handler for this reward already exists." });
            }

            ChannelPointReward reward = new()
            {
                TwitchRewardId = request.TwitchRewardId,
                Title = request.Title?.Trim() ?? "",
                Cost = request.Cost ?? 0,
                ActionType = request.ActionType ?? RewardActionType.ChatMessage,
                ActionPayload = request.ActionPayload?.Trim() ?? "",
                AutoFulfill = request.AutoFulfill ?? true,
                IsEnabled = true
            };

            reward = await repo.CreateAsync(reward, ct);
            return Results.Created($"/api/channel-points/{reward.Id}", reward);
        });

        group.MapPut("/{id:int}", async (int id, UpdateChannelPointHandlerRequest request, IChannelPointRewardRepository repo, CancellationToken ct) =>
        {
            ChannelPointReward? reward = await repo.GetAllAsync(ct) is { } all
                ? System.Linq.Enumerable.FirstOrDefault(all, r => r.Id == id)
                : null;

            if (reward is null)
            {
                return Results.NotFound();
            }

            if (request.Title is not null)
            {
                reward.Title = request.Title;
            }
            if (request.ActionType.HasValue)
            {
                reward.ActionType = request.ActionType.Value;
            }
            if (request.ActionPayload is not null)
            {
                reward.ActionPayload = request.ActionPayload;
            }
            if (request.AutoFulfill.HasValue)
            {
                reward.AutoFulfill = request.AutoFulfill.Value;
            }
            if (request.IsEnabled.HasValue)
            {
                reward.IsEnabled = request.IsEnabled.Value;
            }

            await repo.UpdateAsync(reward, ct);
            return Results.Ok(reward);
        });

        group.MapDelete("/{id:int}", async (int id, IChannelPointRewardRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        group.MapGet("/rewards", async (ITwitchHelixClient helix, CancellationToken ct) =>
        {
            IReadOnlyList<TwitchCustomReward> rewards = await helix.GetCustomRewardsAsync(ct);
            return Results.Ok(rewards);
        });
    }
}

/// <summary>Request payload for creating a new channel point reward handler.</summary>
public record CreateChannelPointHandlerRequest(
    string TwitchRewardId,
    string? Title,
    int? Cost,
    RewardActionType? ActionType,
    string? ActionPayload,
    bool? AutoFulfill);

/// <summary>Request payload for updating an existing channel point reward handler.</summary>
public record UpdateChannelPointHandlerRequest(
    string? Title,
    RewardActionType? ActionType,
    string? ActionPayload,
    bool? AutoFulfill,
    bool? IsEnabled);
