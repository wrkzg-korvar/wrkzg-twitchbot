using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Wrkzg.Api.Security;

/// <summary>
/// Middleware that validates the <c>X-Wrkzg-Token</c> header on all API and hub requests.
/// Exemptions:
///   - Static files (no path prefix match)
///   - <c>/auth/callback</c> — called from the external browser after Twitch OAuth
///   - Requests with a valid token in the <c>access_token</c> query param (SignalR WebSocket negotiation)
/// </summary>
public sealed class ApiTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiTokenService _tokenService;

    private const string TokenHeaderName = "X-Wrkzg-Token";

    public ApiTokenMiddleware(RequestDelegate next, ApiTokenService tokenService)
    {
        _next = next;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        // Exempt: OAuth callback (called from external browser, not the Photino window)
        // Exempt: OAuth redirect endpoints (system browser, no token)
        // Exempt: OBS overlay routes (localhost only, no auth needed)
        if (path.StartsWith("/auth/callback", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/twitch/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/overlay/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/overlays/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Exempt: SignalR connections from overlay clients (source=overlay query param)
        if (path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase))
        {
            string? source = context.Request.Query["source"];
            if (string.Equals(source, "overlay", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        // Only protect API, hub, and auth routes
        bool isProtectedRoute = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                                || path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase)
                                || path.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase);

        if (!isProtectedRoute)
        {
            // Static files, index.html, etc. — pass through
            await _next(context);
            return;
        }

        // Check header first
        string? headerToken = context.Request.Headers[TokenHeaderName];
        if (_tokenService.IsValid(headerToken))
        {
            await _next(context);
            return;
        }

        // Fallback: check query param (used by SignalR WebSocket connections)
        string? queryToken = context.Request.Query["access_token"];
        if (_tokenService.IsValid(queryToken))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"Missing or invalid API token.\"}");
    }
}
