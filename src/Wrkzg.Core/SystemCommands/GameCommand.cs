using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Changes the stream category/game via Twitch Helix API.
/// Usage: !game Crimson Desert
/// Requires: channel:manage:broadcast scope on Broadcaster token.
/// </summary>
public class GameCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!game";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!category" };

    /// <inheritdoc />
    public string Description => "Changes the stream category. Usage: !game Category Name";

    /// <inheritdoc />
    public string? DefaultResponseTemplate => null;

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public GameCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        // Only moderators and broadcaster can change the category
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return null;
        }

        // Extract args after the trigger
        string[] parts = message.Content.Split(' ', 2);
        string args = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(args))
        {
            return "Usage: !game Category Name";
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

        // Resolve game name to game ID
        TwitchGameInfo? game = await helix.GetGameByNameAsync(args, ct);
        if (game is null)
        {
            return $"Category '{args}' not found. Check the name.";
        }

        bool success = await helix.ModifyChannelInfoAsync(broadcasterId, title: null, gameId: game.Id, ct);
        return success
            ? $"Category changed to: {game.Name}"
            : "Failed to change category. Check permissions.";
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
