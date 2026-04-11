using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Wrkzg.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        ProblemDetails problemDetails = exception switch
        {
            ArgumentException argEx => new ProblemDetails
            {
                Type = "https://wrkzg.app/problems/validation-error",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = argEx.Message,
            },
            KeyNotFoundException => new ProblemDetails
            {
                Type = "https://wrkzg.app/problems/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = exception.Message,
            },
            InvalidOperationException when exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) => new ProblemDetails
            {
                Type = "https://wrkzg.app/problems/conflict",
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = exception.Message,
            },
            HttpRequestException => new ProblemDetails
            {
                Type = "https://wrkzg.app/problems/twitch-api-error",
                Title = "External API Error",
                Status = StatusCodes.Status502BadGateway,
                Detail = "An external service (Twitch API) is unavailable. Please try again.",
            },
            _ => new ProblemDetails
            {
                Type = "https://wrkzg.app/problems/internal-error",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred.",
            }
        };

        problemDetails.Instance = httpContext.Request.Path;

        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
