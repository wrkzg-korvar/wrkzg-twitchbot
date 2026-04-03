using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Wrkzg.Api.Security;

/// <summary>
/// Adds standard security headers to all responses.
/// Overlay routes get relaxed headers to allow iframe embedding and OBS Browser Source usage.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        IHeaderDictionary headers = context.Response.Headers;
        string path = context.Request.Path.Value ?? string.Empty;

        bool isOverlayRoute = path.StartsWith("/overlay/", StringComparison.OrdinalIgnoreCase);

        // Prevent MIME-type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Never send Referrer to external sites
        headers["Referrer-Policy"] = "no-referrer";

        if (isOverlayRoute)
        {
            // Overlays must always revalidate — prevents browsers from caching
            // error responses (e.g. 403 when server was briefly down).
            headers["Cache-Control"] = "no-cache";

            // Overlays: allow embedding in iframes (dashboard preview + OBS Browser Source)
            // No X-Frame-Options header → allow framing from same origin
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "connect-src 'self' ws://localhost:5050 ws://localhost:5173; " +
                "img-src 'self' data: https://static-cdn.jtvnw.net https://img.youtube.com https://i.ytimg.com; " +
                "font-src 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'self';";
        }
        else
        {
            // Dashboard + API: strict security headers
            headers["X-Frame-Options"] = "DENY";

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "connect-src 'self' ws://localhost:5050 ws://localhost:5173; " +
                "img-src 'self' data: https://static-cdn.jtvnw.net https://img.youtube.com https://i.ytimg.com; " +
                "font-src 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none';";
        }

        await _next(context);
    }
}
