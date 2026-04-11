using System;
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
/// REST endpoints for timed/recurring chat messages.
/// </summary>
public static class TimerEndpoints
{
    /// <summary>Registers timed message CRUD API endpoints.</summary>
    public static void MapTimerEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/timers").WithTags("Timers");

        group.MapGet("/", async (ITimedMessageRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<TimedMessage> timers = await repo.GetAllAsync(ct);
            return Results.Ok(timers);
        });

        group.MapGet("/{id:int}", async (int id, ITimedMessageRepository repo, CancellationToken ct) =>
        {
            TimedMessage? timer = await repo.GetByIdAsync(id, ct);
            return timer is not null ? Results.Ok(timer) : TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
        });

        group.MapPost("/", async (CreateTimerRequest request, ITimedMessageRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return TypedResults.Problem(detail: "Timer needs a name.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }
            if (request.Messages is null || request.Messages.Length == 0)
            {
                return TypedResults.Problem(detail: "Timer needs at least one message.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }
            if (request.IntervalMinutes < 1 || request.IntervalMinutes > 1440)
            {
                return TypedResults.Problem(detail: "Interval must be 1-1440 minutes.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            string[] validColors = new[] { "primary", "blue", "green", "orange", "purple" };
            if (!string.IsNullOrWhiteSpace(request.AnnouncementColor) &&
                !Array.Exists(validColors, c => string.Equals(c, request.AnnouncementColor, StringComparison.OrdinalIgnoreCase)))
            {
                return TypedResults.Problem(detail: "Invalid announcement color. Allowed: primary, blue, green, orange, purple.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            TimedMessage timer = new()
            {
                Name = request.Name.Trim(),
                Messages = request.Messages,
                IntervalMinutes = request.IntervalMinutes,
                MinChatLines = request.MinChatLines ?? 5,
                IsEnabled = request.IsEnabled ?? true,
                RunWhenOnline = request.RunWhenOnline ?? true,
                RunWhenOffline = request.RunWhenOffline ?? false,
                IsAnnouncement = request.IsAnnouncement ?? false,
                AnnouncementColor = string.IsNullOrWhiteSpace(request.AnnouncementColor) ? "primary" : request.AnnouncementColor
            };

            timer = await repo.CreateAsync(timer, ct);
            return Results.Created($"/api/timers/{timer.Id}", timer);
        });

        group.MapPut("/{id:int}", async (int id, UpdateTimerRequest request, ITimedMessageRepository repo, CancellationToken ct) =>
        {
            TimedMessage? timer = await repo.GetByIdAsync(id, ct);
            if (timer is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            if (request.Name is not null)
            {
                timer.Name = request.Name;
            }
            if (request.Messages is not null)
            {
                timer.Messages = request.Messages;
            }
            if (request.IntervalMinutes.HasValue)
            {
                if (request.IntervalMinutes.Value < 1 || request.IntervalMinutes.Value > 1440)
                {
                    return TypedResults.Problem(detail: "Interval must be between 1 and 1440 minutes.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
                }

                timer.IntervalMinutes = request.IntervalMinutes.Value;
            }
            if (request.MinChatLines.HasValue)
            {
                timer.MinChatLines = request.MinChatLines.Value;
            }
            if (request.IsEnabled.HasValue)
            {
                timer.IsEnabled = request.IsEnabled.Value;
            }
            if (request.RunWhenOnline.HasValue)
            {
                timer.RunWhenOnline = request.RunWhenOnline.Value;
            }
            if (request.RunWhenOffline.HasValue)
            {
                timer.RunWhenOffline = request.RunWhenOffline.Value;
            }
            if (request.IsAnnouncement.HasValue)
            {
                timer.IsAnnouncement = request.IsAnnouncement.Value;
            }
            if (request.AnnouncementColor is not null)
            {
                if (string.IsNullOrWhiteSpace(request.AnnouncementColor))
                {
                    timer.AnnouncementColor = "primary";
                }
                else
                {
                    string[] validColors = new[] { "primary", "blue", "green", "orange", "purple" };
                    if (!Array.Exists(validColors, c => string.Equals(c, request.AnnouncementColor, StringComparison.OrdinalIgnoreCase)))
                    {
                        return TypedResults.Problem(detail: "Invalid announcement color. Allowed: primary, blue, green, orange, purple.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
                    }
                    timer.AnnouncementColor = request.AnnouncementColor;
                }
            }

            await repo.UpdateAsync(timer, ct);
            return Results.Ok(timer);
        });

        group.MapDelete("/{id:int}", async (int id, ITimedMessageRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}

/// <summary>Request payload for creating a new timed message.</summary>
public record CreateTimerRequest(
    string Name,
    string[] Messages,
    int IntervalMinutes,
    int? MinChatLines,
    bool? IsEnabled,
    bool? RunWhenOnline,
    bool? RunWhenOffline,
    bool? IsAnnouncement,
    string? AnnouncementColor);

/// <summary>Request payload for updating an existing timed message.</summary>
public record UpdateTimerRequest(
    string? Name,
    string[]? Messages,
    int? IntervalMinutes,
    int? MinChatLines,
    bool? IsEnabled,
    bool? RunWhenOnline,
    bool? RunWhenOffline,
    bool? IsAnnouncement,
    string? AnnouncementColor);
