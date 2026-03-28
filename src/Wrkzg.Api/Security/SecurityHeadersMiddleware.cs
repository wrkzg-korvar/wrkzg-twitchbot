using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Wrkzg.Api.Security;

/// <summary>
/// Adds standard security headers to all responses.
/// Defense-in-depth for the Photino desktop WebView.
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

        // Prevent MIME-type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking — app should never be embedded in a frame
        headers["X-Frame-Options"] = "DENY";

        // Never send Referrer to external sites
        headers["Referrer-Policy"] = "no-referrer";

        // Content Security Policy — restrict script/style/connect sources
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "connect-src 'self' ws://localhost:5000 ws://localhost:5173; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none';";

        await _next(context);
    }
}
