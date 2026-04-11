using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Changes the stream title via Twitch Helix API.
/// Usage: !titel Neuer Stream Titel hier
/// Requires: channel:manage:broadcast scope on Broadcaster token.
/// </summary>
public class TitleCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!titel";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!title" };

    /// <inheritdoc />
    public string Description => "Changes the stream title. Usage: !titel New Title Here";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitleCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public TitleCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        // Only moderators and broadcaster can change the title
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return null;
        }

        // Extract args after the trigger
        string[] parts = message.Content.Split(' ', 2);
        string args = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(args))
        {
            return "Usage: !titel New Stream Title";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IBroadcasterHelixClient helix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
        ISecureStorage storage = scope.ServiceProvider.GetRequiredService<ISecureStorage>();
        ITwitchOAuthService oauth = scope.ServiceProvider.GetRequiredService<ITwitchOAuthService>();

        string? broadcasterId = await ResolveBroadcasterIdAsync(storage, oauth, ct);
        if (broadcasterId is null)
        {
            return "Broadcaster not connected.";
        }

        bool success = await helix.ModifyChannelInfoAsync(broadcasterId, title: args, gameId: null, ct);
        return success
            ? $"Title changed to: {args}"
            : "Failed to change title. Check permissions.";
    }

    private static async Task<string?> ResolveBroadcasterIdAsync(
        ISecureStorage storage, ITwitchOAuthService oauth, CancellationToken ct)
    {
        TwitchTokens? tokens = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
        if (tokens is null)
        {
            return null;
        }

        TwitchTokenValidation? v = await oauth.ValidateTokenAsync(tokens.AccessToken, ct);
        return v?.UserId;
    }
}
