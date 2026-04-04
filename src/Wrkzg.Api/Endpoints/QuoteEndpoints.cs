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
/// REST endpoints for managing saved chat quotes.
/// </summary>
public static class QuoteEndpoints
{
    /// <summary>Registers chat quote CRUD API endpoints.</summary>
    public static void MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/quotes").WithTags("Quotes");

        group.MapGet("/", async (IQuoteRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<Quote> quotes = await repo.GetAllAsync(ct);
            return Results.Ok(quotes);
        });

        group.MapGet("/{id:int}", async (int id, IQuoteRepository repo, CancellationToken ct) =>
        {
            Quote? quote = await repo.GetByIdAsync(id, ct);
            return quote is not null ? Results.Ok(quote) : Results.NotFound();
        });

        group.MapGet("/random", async (IQuoteRepository repo, CancellationToken ct) =>
        {
            Quote? quote = await repo.GetRandomAsync(ct);
            return quote is not null ? Results.Ok(quote) : Results.NotFound();
        });

        group.MapPost("/", async (CreateQuoteRequest request, IQuoteRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new { error = "Quote text is required." });
            }

            if (request.Text.Length > 500)
            {
                return Results.BadRequest(new { error = "Quote text must be 500 characters or less." });
            }

            if (string.IsNullOrWhiteSpace(request.QuotedUser))
            {
                return Results.BadRequest(new { error = "QuotedUser is required." });
            }

            int nextNumber = await repo.GetNextNumberAsync(ct);
            Quote quote = new()
            {
                Number = nextNumber,
                Text = request.Text.Trim(),
                QuotedUser = request.QuotedUser.Trim(),
                SavedBy = request.SavedBy?.Trim() ?? "Dashboard",
                GameName = request.GameName?.Trim()
            };

            Quote created = await repo.CreateAsync(quote, ct);
            return Results.Created($"/api/quotes/{created.Id}", created);
        });

        group.MapDelete("/{id:int}", async (int id, IQuoteRepository repo, CancellationToken ct) =>
        {
            Quote? quote = await repo.GetByIdAsync(id, ct);
            if (quote is null)
            {
                return Results.NotFound();
            }

            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}

/// <summary>Request body for creating a new quote.</summary>
public sealed record CreateQuoteRequest(
    string Text,
    string QuotedUser,
    string? SavedBy = null,
    string? GameName = null
);
