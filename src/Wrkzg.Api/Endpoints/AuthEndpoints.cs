using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for Twitch OAuth authentication and credential management.
/// </summary>
public static class AuthEndpoints
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _pendingStates = new();
    private static readonly TimeSpan StateMaxAge = TimeSpan.FromMinutes(10);

    /// <summary>Registers Twitch OAuth authentication API endpoints.</summary>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Authentication");

        group.MapGet("/twitch/{type}", StartOAuthFlow);
        group.MapGet("/url/{type}", GetOAuthUrl);
        group.MapGet("/callback", HandleCallback);
        group.MapGet("/status", GetAuthStatus);
        group.MapPost("/disconnect/{type}", Disconnect);
        group.MapPost("/credentials", SaveCredentials);
        group.MapPost("/open-browser/{type}", OpenBrowserForOAuth);
        group.MapGet("/setup-status", GetSetupStatus);
    }

    private static IResult StartOAuthFlow(
        string type,
        ITwitchOAuthService oauth,
        ILogger<ITwitchOAuthService> logger)
    {
        if (!TryParseTokenType(type, out TokenType tokenType))
        {
            return TypedResults.Problem(detail: $"Invalid token type: '{type}'. Use 'bot' or 'broadcaster'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        }

        CleanupExpiredStates();

        (string url, string state) = oauth.BuildAuthorizationUrl(tokenType);
        _pendingStates[state] = DateTimeOffset.UtcNow;

        logger.LogInformation("Starting OAuth flow for {TokenType} — redirecting to Twitch", tokenType);

        return Results.Redirect(url);
    }

    private static IResult GetOAuthUrl(
        string type,
        ITwitchOAuthService oauth,
        ILogger<ITwitchOAuthService> logger)
    {
        if (!TryParseTokenType(type, out TokenType tokenType))
        {
            return TypedResults.Problem(detail: $"Invalid token type: '{type}'. Use 'bot' or 'broadcaster'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        }

        CleanupExpiredStates();

        (string url, string state) = oauth.BuildAuthorizationUrl(tokenType);
        _pendingStates[state] = DateTimeOffset.UtcNow;

        logger.LogInformation("Generated OAuth URL for {TokenType} — to be opened in system browser", tokenType);

        return Results.Ok(new { url });
    }

    private static async Task<IResult> HandleCallback(
        HttpContext context,
        ITwitchOAuthService oauth,
        ISecureStorage storage,
        IAuthStateNotifier notifier,
        IEmoteService emoteService,
        ILogger<ITwitchOAuthService> logger,
        CancellationToken ct)
    {
        string? code = context.Request.Query["code"];
        string? state = context.Request.Query["state"];
        string? error = context.Request.Query["error"];
        string? errorDescription = context.Request.Query["error_description"];

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("OAuth denied by user: {Error} — {Description}", error, errorDescription);
            return Results.Content(
                BuildCallbackHtml(success: false, message: $"Authorization denied: {errorDescription ?? error}"),
                "text/html");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return TypedResults.Problem(detail: "Missing code or state parameter.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        }

        if (!_pendingStates.TryRemove(state, out DateTimeOffset createdAt))
        {
            logger.LogWarning("OAuth callback with unknown or already-used state");
            return Results.Content(
                BuildCallbackHtml(success: false, message: "Invalid or expired authorization state. Please try again."),
                "text/html");
        }

        if (DateTimeOffset.UtcNow - createdAt > StateMaxAge)
        {
            return Results.Content(
                BuildCallbackHtml(success: false, message: "Authorization timed out. Please try again."),
                "text/html");
        }

        int colonIndex = state.IndexOf(':');
        if (colonIndex < 0 || !TryParseTokenType(state[..colonIndex], out TokenType tokenType))
        {
            return Results.Content(
                BuildCallbackHtml(success: false, message: "Invalid state format."),
                "text/html");
        }

        try
        {
            TwitchTokens tokens = await oauth.ExchangeCodeAsync(code, ct);
            await storage.SaveTokensAsync(tokenType, tokens, ct);

            logger.LogInformation("OAuth flow completed for {TokenType} — tokens saved", tokenType);

            TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(tokens.AccessToken, ct);

            await notifier.NotifyAuthStateChangedAsync(new AuthState
            {
                TokenType = tokenType,
                IsAuthenticated = true,
                TwitchUsername = validation?.Login,
                TwitchDisplayName = validation?.Login,
                TwitchUserId = validation?.UserId,
                Scopes = tokens.Scope
            }, ct);

            // Trigger emote cache refresh after new auth
            try
            {
                await emoteService.RefreshAsync(ct);
                logger.LogInformation("Emote cache refreshed after {TokenType} auth", tokenType);
            }
            catch (Exception emoteEx)
            {
                logger.LogWarning(emoteEx, "Emote cache refresh failed after {TokenType} auth — emotes will load on next timer tick or manual refresh", tokenType);
            }

            return Results.Content(
                BuildCallbackHtml(success: true, message: $"{tokenType} account connected successfully!"),
                "text/html");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Token exchange failed for {TokenType}", tokenType);
            return Results.Content(
                BuildCallbackHtml(success: false, message: "Token exchange failed. Please check your Twitch app credentials and try again."),
                "text/html");
        }
    }

    private static async Task<IResult> GetAuthStatus(
        ISecureStorage storage,
        ITwitchOAuthService oauth,
        CancellationToken ct)
    {
        AuthState botState = await BuildAuthStateAsync(TokenType.Bot, storage, oauth, ct);
        AuthState broadcasterState = await BuildAuthStateAsync(TokenType.Broadcaster, storage, oauth, ct);

        return Results.Ok(new { bot = botState, broadcaster = broadcasterState });
    }

    private static async Task<IResult> Disconnect(
        string type,
        ISecureStorage storage,
        ITwitchOAuthService oauth,
        IAuthStateNotifier notifier,
        ILogger<ITwitchOAuthService> logger,
        CancellationToken ct)
    {
        if (!TryParseTokenType(type, out TokenType tokenType))
        {
            return TypedResults.Problem(detail: $"Invalid token type: '{type}'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        }

        TwitchTokens? tokens = await storage.LoadTokensAsync(tokenType, ct);
        if (tokens is not null)
        {
            try
            {
                await oauth.RevokeTokenAsync(tokens.AccessToken, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Token revocation failed for {TokenType} — continuing with local deletion", tokenType);
            }
        }

        await storage.DeleteTokensAsync(tokenType, ct);

        await notifier.NotifyAuthStateChangedAsync(new AuthState
        {
            TokenType = tokenType,
            IsAuthenticated = false
        }, ct);

        logger.LogInformation("{TokenType} account disconnected", tokenType);
        return Results.Ok(new { disconnected = true, tokenType = tokenType.ToString() });
    }

    private static async Task<IResult> SaveCredentials(
        SaveCredentialsRequest request,
        ISecureStorage storage,
        ILogger<ITwitchOAuthService> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return TypedResults.Problem(detail: "clientSecret is required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        }

        // If clientId is provided, save it. Otherwise keep the existing one.
        if (!string.IsNullOrWhiteSpace(request.ClientId))
        {
            await storage.SaveClientIdAsync(request.ClientId.Trim(), ct);
        }
        else
        {
            // Verify that an existing clientId is stored
            string? existingId = await storage.LoadClientIdAsync(ct);
            if (string.IsNullOrWhiteSpace(existingId))
            {
                return TypedResults.Problem(detail: "clientId is required (no existing clientId found).", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }
        }

        await storage.SaveClientSecretAsync(request.ClientSecret.Trim(), ct);

        logger.LogInformation("Twitch app credentials saved to secure storage");

        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> GetSetupStatus(
        ISecureStorage storage,
        ISettingsRepository settings,
        CancellationToken ct)
    {
        bool hasCredentials = await storage.HasCredentialsAsync(ct);
        TwitchTokens? botToken = await storage.LoadTokensAsync(TokenType.Bot, ct);
        TwitchTokens? broadcasterToken = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
        string? channel = await settings.GetAsync("Bot.Channel", ct);
        bool hasChannel = !string.IsNullOrWhiteSpace(channel);

        return Results.Ok(new
        {
            hasCredentials,
            hasBotToken = botToken is not null,
            hasBroadcasterToken = broadcasterToken is not null,
            hasChannel,
            setupComplete = hasCredentials && botToken is not null && broadcasterToken is not null && hasChannel
        });
    }

    /// <summary>
    /// POST /auth/open-browser/bot  or  POST /auth/open-browser/broadcaster
    /// Generates the Twitch auth URL, stores the state, and opens the system browser.
    /// Returns 200 OK immediately — the browser opens asynchronously.
    /// </summary>
    private static IResult OpenBrowserForOAuth(
        string type,
        ITwitchOAuthService oauth,
        ILogger<ITwitchOAuthService> logger)
    {
        if (!TryParseTokenType(type, out TokenType tokenType))
        {
            return TypedResults.Problem(detail: $"Invalid token type: '{type}'. Use 'bot' or 'broadcaster'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        }

        CleanupExpiredStates();

        (string url, string state) = oauth.BuildAuthorizationUrl(tokenType);
        _pendingStates[state] = DateTimeOffset.UtcNow;

        // Open the system default browser
        try
        {
            OpenUrlInBrowser(url);
            logger.LogInformation("Opened system browser for {TokenType} OAuth", tokenType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open system browser");
            return Results.StatusCode(500);
        }

        return Results.Ok(new { opened = true });
    }

    /// <summary>
    /// Opens a URL in the operating system's default browser.
    /// Works on Windows, macOS, and Linux.
    /// </summary>
    private static void OpenUrlInBrowser(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows: UseShellExecute with a URL is safe (shell handles it directly)
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            // macOS: use ArgumentList to prevent argument injection
            ProcessStartInfo psi = new() { FileName = "open", UseShellExecute = false };
            psi.ArgumentList.Add(url);
            Process.Start(psi);
        }
        else if (OperatingSystem.IsLinux())
        {
            // Linux: use ArgumentList to prevent argument injection
            ProcessStartInfo psi = new() { FileName = "xdg-open", UseShellExecute = false };
            psi.ArgumentList.Add(url);
            Process.Start(psi);
        }
        else
        {
            throw new PlatformNotSupportedException("Cannot open browser on this OS.");
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static async Task<AuthState> BuildAuthStateAsync(
        TokenType tokenType, ISecureStorage storage, ITwitchOAuthService oauth, CancellationToken ct)
    {
        TwitchTokens? tokens = await storage.LoadTokensAsync(tokenType, ct);
        if (tokens is null)
        {
            return new AuthState { TokenType = tokenType, IsAuthenticated = false };
        }

        TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(tokens.AccessToken, ct);

        if (validation is null)
        {
            try
            {
                TwitchTokens newTokens = await oauth.RefreshTokenAsync(tokens.RefreshToken, ct);
                await storage.SaveTokensAsync(tokenType, newTokens, ct);
                validation = await oauth.ValidateTokenAsync(newTokens.AccessToken, ct);
            }
            catch
            {
                return new AuthState { TokenType = tokenType, IsAuthenticated = false };
            }
        }

        return new AuthState
        {
            TokenType = tokenType,
            IsAuthenticated = validation is not null,
            TwitchUsername = validation?.Login,
            TwitchDisplayName = validation?.Login,
            TwitchUserId = validation?.UserId,
            Scopes = validation?.Scopes
        };
    }

    private static bool TryParseTokenType(string value, out TokenType result)
    {
        return Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);
    }

    private static void CleanupExpiredStates()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - StateMaxAge;
        foreach (KeyValuePair<string, DateTimeOffset> kvp in _pendingStates)
        {
            if (kvp.Value < cutoff)
            {
                _pendingStates.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string BuildCallbackHtml(bool success, string message)
    {
        string bgColor = success ? "#10b981" : "#ef4444";
        string icon = success ? "✓" : "✗";
        string title = success ? "Connected!" : "Connection Failed";
        // HTML-encode to prevent XSS via error_description or other user-controlled values
        string safeMessage = WebUtility.HtmlEncode(message);
        string safeTitle = WebUtility.HtmlEncode(title);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Wrkzg — {{safeTitle}}</title>
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body {
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
                        background: #0f172a; color: #f8fafc;
                        display: flex; align-items: center; justify-content: center; min-height: 100vh;
                    }
                    .card { text-align: center; padding: 3rem 2rem; max-width: 420px; }
                    .icon {
                        width: 64px; height: 64px; border-radius: 50%; background: {{bgColor}};
                        display: flex; align-items: center; justify-content: center;
                        font-size: 32px; margin: 0 auto 1.5rem;
                    }
                    h1 { font-size: 1.5rem; margin-bottom: 0.75rem; }
                    p { color: #94a3b8; line-height: 1.6; }
                    .hint { margin-top: 2rem; font-size: 0.85rem; color: #64748b; }
                </style>
            </head>
            <body>
                <div class="card">
                    <div class="icon">{{icon}}</div>
                    <h1>{{safeTitle}}</h1>
                    <p>{{safeMessage}}</p>
                    <p class="hint">
                        {{(success
                            ? "You can close this browser tab and return to the Wrkzg app."
                            : "Close this tab and try again from the Wrkzg app.")}}
                    </p>
                </div>
            </body>
            </html>
            """;
    }
}

/// <summary>Request payload for saving Twitch application credentials.</summary>
public sealed record SaveCredentialsRequest(string ClientId, string ClientSecret);
