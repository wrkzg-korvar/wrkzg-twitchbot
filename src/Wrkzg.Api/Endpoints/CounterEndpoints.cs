using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for named counters (deaths, wins, etc.).
/// </summary>
public static class CounterEndpoints
{
    /// <summary>Registers named counter CRUD and increment/decrement API endpoints.</summary>
    public static void MapCounterEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/counters").WithTags("Counters");

        group.MapGet("/", async (ICounterRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<Counter> counters = await repo.GetAllAsync(ct);
            return Results.Ok(counters);
        });

        group.MapGet("/{id:int}", async (int id, ICounterRepository repo, CancellationToken ct) =>
        {
            Counter? counter = await repo.GetByIdAsync(id, ct);
            return counter is not null ? Results.Ok(counter) : Results.NotFound();
        });

        group.MapPost("/", async (CreateCounterRequest request, ICounterRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Counter needs a name." });
            }

            string trigger = "!" + request.Name.Trim().ToLowerInvariant().Replace(" ", "");

            Counter? existing = await repo.GetByTriggerAsync(trigger, ct);
            if (existing is not null)
            {
                return Results.BadRequest(new { error = $"A counter with trigger {trigger} already exists." });
            }

            Counter counter = new()
            {
                Name = request.Name.Trim(),
                Trigger = trigger,
                Value = request.Value ?? 0,
                ResponseTemplate = request.ResponseTemplate ?? "{name}: {value}"
            };

            counter = await repo.CreateAsync(counter, ct);
            return Results.Created($"/api/counters/{counter.Id}", counter);
        });

        group.MapPut("/{id:int}", async (int id, UpdateCounterRequest request, ICounterRepository repo, CancellationToken ct) =>
        {
            Counter? counter = await repo.GetByIdAsync(id, ct);
            if (counter is null)
            {
                return Results.NotFound();
            }

            if (request.Name is not null)
            {
                counter.Name = request.Name;
                counter.Trigger = "!" + request.Name.Trim().ToLowerInvariant().Replace(" ", "");
            }
            if (request.Value.HasValue)
            {
                counter.Value = request.Value.Value;
            }
            if (request.ResponseTemplate is not null)
            {
                counter.ResponseTemplate = request.ResponseTemplate;
            }

            await repo.UpdateAsync(counter, ct);
            return Results.Ok(counter);
        });

        group.MapDelete("/{id:int}", async (int id, ICounterRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:int}/increment", async (int id, ICounterRepository repo, IChatEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            Counter? counter = await repo.GetByIdAsync(id, ct);
            if (counter is null)
            {
                return Results.NotFound();
            }
            counter.Value++;
            await repo.UpdateAsync(counter, ct);
            await broadcaster.BroadcastCounterUpdatedAsync(counter.Id, counter.Name, counter.Value, ct);
            return Results.Ok(counter);
        });

        group.MapPost("/{id:int}/decrement", async (int id, ICounterRepository repo, IChatEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            Counter? counter = await repo.GetByIdAsync(id, ct);
            if (counter is null)
            {
                return Results.NotFound();
            }
            counter.Value--;
            await repo.UpdateAsync(counter, ct);
            await broadcaster.BroadcastCounterUpdatedAsync(counter.Id, counter.Name, counter.Value, ct);
            return Results.Ok(counter);
        });

        group.MapPost("/{id:int}/reset", async (int id, ICounterRepository repo, IChatEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            Counter? counter = await repo.GetByIdAsync(id, ct);
            if (counter is null)
            {
                return Results.NotFound();
            }
            counter.Value = 0;
            await repo.UpdateAsync(counter, ct);
            await broadcaster.BroadcastCounterUpdatedAsync(counter.Id, counter.Name, counter.Value, ct);
            return Results.Ok(counter);
        });
    }
}

/// <summary>Request payload for creating a new named counter.</summary>
public record CreateCounterRequest(string Name, int? Value, string? ResponseTemplate);

/// <summary>Request payload for updating an existing counter.</summary>
public record UpdateCounterRequest(string? Name, int? Value, string? ResponseTemplate);
