using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for Chat Games configuration.
/// </summary>
public static class GameEndpoints
{
    /// <summary>Registers chat game configuration and trivia question API endpoints.</summary>
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/games").WithTags("Games");

        group.MapGet("/", (ChatGameManager manager) =>
        {
            List<object> games = manager.GetAllGames().Select(g => new
            {
                name = g.Name,
                trigger = g.Trigger,
                aliases = g.Aliases,
                description = g.Description,
                isEnabled = g.IsEnabled,
                minRolePriority = g.MinRolePriority
            }).ToList<object>();

            return Results.Ok(games);
        });

        group.MapPost("/{name}/toggle", (string name, ChatGameManager manager) =>
        {
            IChatGame? game = manager.GetAllGames()
                .FirstOrDefault(g => string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase));

            if (game is null)
            {
                return TypedResults.Problem(detail: $"Game '{name}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            game.IsEnabled = !game.IsEnabled;
            return Results.Ok(new { name = game.Name, isEnabled = game.IsEnabled });
        });

        group.MapPut("/{name}/settings", async (string name, UpdateGameSettingsRequest request,
            ISettingsRepository settings, ChatGameManager manager, CancellationToken ct) =>
        {
            IChatGame? game = manager.GetAllGames()
                .FirstOrDefault(g => string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase));

            if (game is null)
            {
                return TypedResults.Problem(detail: $"Game '{name}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            // Save each setting under Games.{Name}.{Key}
            if (request.Settings is not null)
            {
                foreach (KeyValuePair<string, string> kvp in request.Settings)
                {
                    string key = $"Games.{game.Name}.{kvp.Key}";
                    await settings.SetAsync(key, kvp.Value, ct);
                }
            }

            return Results.Ok(new { name = game.Name, updated = request.Settings?.Count ?? 0 });
        });

        // Message templates per game — reads directly from DB to get current values
        group.MapGet("/{name}/messages", async (string name, ChatGameManager manager,
            ISettingsRepository settings, CancellationToken ct) =>
        {
            IChatGame? game = manager.GetAllGames()
                .FirstOrDefault(g => string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase));

            if (game is null)
            {
                return TypedResults.Problem(detail: $"Game '{name}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            Dictionary<string, string> defaults = game.GetDefaultMessageTemplates();
            Dictionary<string, string> current = new();

            foreach (KeyValuePair<string, string> kvp in defaults)
            {
                string settingsKey = $"Games.{game.Name}.Msg.{kvp.Key}";
                string? custom = await settings.GetAsync(settingsKey, ct);
                current[kvp.Key] = !string.IsNullOrWhiteSpace(custom) ? custom : kvp.Value;
            }

            return Results.Ok(new
            {
                name = game.Name,
                messages = current,
                defaults
            });
        });

        group.MapPut("/{name}/messages", async (string name, UpdateGameMessagesRequest request,
            ISettingsRepository settings, ChatGameManager manager, CancellationToken ct) =>
        {
            IChatGame? game = manager.GetAllGames()
                .FirstOrDefault(g => string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase));

            if (game is null)
            {
                return TypedResults.Problem(detail: $"Game '{name}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            if (request.Messages is not null)
            {
                foreach (KeyValuePair<string, string> kvp in request.Messages)
                {
                    string key = $"Games.{game.Name}.Msg.{kvp.Key}";
                    await settings.SetAsync(key, kvp.Value, ct);
                }
            }

            return Results.Ok(new { name = game.Name, updated = request.Messages?.Count ?? 0 });
        });

        group.MapPost("/{name}/messages/{messageKey}/reset", async (string name, string messageKey,
            ISettingsRepository settings, ChatGameManager manager, CancellationToken ct) =>
        {
            IChatGame? game = manager.GetAllGames()
                .FirstOrDefault(g => string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase));

            if (game is null)
            {
                return TypedResults.Problem(detail: $"Game '{name}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            Dictionary<string, string> defaults = game.GetDefaultMessageTemplates();
            if (!defaults.ContainsKey(messageKey))
            {
                return TypedResults.Problem(detail: $"Message key '{messageKey}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            // Delete the custom setting so it falls back to default
            string key = $"Games.{game.Name}.Msg.{messageKey}";
            await settings.DeleteAsync(key, ct);

            return Results.Ok(new { name = game.Name, key = messageKey, value = defaults[messageKey] });
        });

        // Trivia question management
        group.MapGet("/trivia/questions", async (ITriviaQuestionRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<TriviaQuestion> questions = await repo.GetCustomAsync(ct);
            return Results.Ok(questions);
        });

        group.MapPost("/trivia/questions", async (CreateTriviaQuestionRequest request,
            ITriviaQuestionRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.Answer))
            {
                return TypedResults.Problem(detail: "Question and answer are required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            TriviaQuestion question = new()
            {
                Question = request.Question.Trim(),
                Answer = request.Answer.Trim(),
                AcceptedAnswers = request.AcceptedAnswers ?? new List<string>(),
                Category = request.Category?.Trim(),
                IsCustom = true
            };

            question = await repo.CreateAsync(question, ct);
            return Results.Created($"/api/games/trivia/questions/{question.Id}", question);
        });

        group.MapDelete("/trivia/questions/{id:int}", async (int id, ITriviaQuestionRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}

/// <summary>Request payload for updating game-specific settings.</summary>
public record UpdateGameSettingsRequest(Dictionary<string, string>? Settings);

/// <summary>Request payload for updating game message templates.</summary>
public record UpdateGameMessagesRequest(Dictionary<string, string>? Messages);

/// <summary>Request payload for creating a custom trivia question.</summary>
public record CreateTriviaQuestionRequest(
    string Question,
    string Answer,
    List<string>? AcceptedAnswers,
    string? Category);
