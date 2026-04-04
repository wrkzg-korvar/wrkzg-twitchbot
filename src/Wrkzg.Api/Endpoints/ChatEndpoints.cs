using System;
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
/// Chat endpoints — recent messages for dashboard reload + send messages.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>Registers chat message retrieval and sending API endpoints.</summary>
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/chat").WithTags("Chat");

        group.MapGet("/recent", (ChatMessageBuffer buffer, string? userId) =>
        {
            // Transform to the same shape as the SignalR broadcast
            var messages = buffer.GetRecent(15, userId).Select(m => new
            {
                userId = m.UserId,
                username = m.Username,
                displayName = m.DisplayName,
                content = m.Content,
                isMod = m.IsModerator,
                isSubscriber = m.IsSubscriber,
                isBroadcaster = m.IsBroadcaster,
                timestamp = m.Timestamp
            });
            return Results.Ok(messages);
        });

        group.MapPost("/send", async (
            SendChatMessageRequest request,
            ITwitchChatClient chatClient,
            ITwitchHelixClient helixClient,
            ISecureStorage storage,
            ITwitchOAuthService oauth,
            IChatEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message cannot be empty." });
            }

            if (request.Message.Length > 500)
            {
                return Results.BadRequest(new { error = "Message too long (max 500 chars)." });
            }

            string trimmedMessage = request.Message.Trim();
            string sendAs = request.SendAs ?? "bot";

            if (sendAs == "bot")
            {
                if (!chatClient.IsConnected)
                {
                    return Results.BadRequest(new { error = "Bot is not connected to chat." });
                }

                await chatClient.SendMessageAsync(trimmedMessage, ct);
            }
            else
            {
                // Send as broadcaster via Helix API
                TwitchTokens? broadcasterTokens = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
                if (broadcasterTokens is null)
                {
                    return Results.BadRequest(new { error = "Broadcaster account not connected." });
                }

                // Get broadcaster user ID from token validation
                TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(broadcasterTokens.AccessToken, ct);
                if (validation is null)
                {
                    return Results.BadRequest(new { error = "Broadcaster token is invalid." });
                }

                bool sent = await helixClient.SendChatMessageAsync(
                    validation.UserId, validation.UserId, trimmedMessage, ct);

                if (!sent)
                {
                    return Results.StatusCode(502);
                }
            }

            // Broadcaster messages come back via IRC (the bot sees all channel messages).
            // Bot self-messages are filtered by TwitchChatClient to prevent command loops,
            // so we manually broadcast only bot-sent messages to the dashboard.
            if (sendAs == "bot")
            {
                TwitchTokens? botTokens = await storage.LoadTokensAsync(TokenType.Bot, ct);
                string botDisplay = "Bot";
                if (botTokens is not null)
                {
                    TwitchTokenValidation? botValidation = await oauth.ValidateTokenAsync(botTokens.AccessToken, ct);
                    if (botValidation is not null)
                    {
                        botDisplay = botValidation.Login;
                    }
                }

                await broadcaster.BroadcastChatMessageAsync(new ChatMessage(
                    UserId: "",
                    Username: botDisplay,
                    DisplayName: botDisplay,
                    Content: trimmedMessage,
                    IsModerator: true,
                    IsSubscriber: false,
                    IsBroadcaster: false,
                    Timestamp: DateTimeOffset.UtcNow), ct);
            }

            return Results.Ok(new { sent = true });
        });
    }
}

/// <summary>Request body for sending a chat message.</summary>
public sealed record SendChatMessageRequest(string Message, string SendAs = "bot");
