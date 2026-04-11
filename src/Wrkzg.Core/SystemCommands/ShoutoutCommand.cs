using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.SystemCommands;

/// <summary>
/// Gives a shoutout to another streamer, including their last played game.
/// Usage: !so @username or !so username
/// Mod + Broadcaster only.
/// </summary>
public class ShoutoutCommand : ISystemCommand
{
    /// <inheritdoc />
    public string Trigger => "!so";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!shoutout" };

    /// <inheritdoc />
    public string Description => "Give a shoutout to another streamer. Usage: !so @username";

    /// <inheritdoc />
    public string? DefaultResponseTemplate =>
        "Go check out {target} at https://twitch.tv/{target_login} — they were last playing {game}!";

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShoutoutCommand"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers.</param>
    public ShoutoutCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(ChatMessage message, CancellationToken ct = default)
    {
        // Permission: Mod + Broadcaster only
        if (!message.IsModerator && !message.IsBroadcaster)
        {
            return null;
        }

        // Parse target username from message
        string args = message.Content.Length > Trigger.Length
            ? message.Content[Trigger.Length..].Trim()
            : string.Empty;

        // Also handle !shoutout prefix
        if (string.IsNullOrEmpty(args) && message.Content.StartsWith("!shoutout", StringComparison.OrdinalIgnoreCase))
        {
            args = message.Content.Length > "!shoutout".Length
                ? message.Content["!shoutout".Length..].Trim()
                : string.Empty;
        }

        if (string.IsNullOrEmpty(args))
        {
            return $"@{message.DisplayName}, usage: !so @username";
        }

        // Strip @ prefix if present
        string targetLogin = args.Split(' ', 2)[0].TrimStart('@').ToLowerInvariant();

        if (string.IsNullOrEmpty(targetLogin))
        {
            return $"@{message.DisplayName}, usage: !so @username";
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IBroadcasterHelixClient helix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();

        // Resolve target user
        HelixUserInfo? targetUser = await helix.GetUserAsync(targetLogin, ct);
        if (targetUser is null)
        {
            return $"@{message.DisplayName}, couldn't find a user named '{targetLogin}'.";
        }

        // Get channel info for the game name
        ChannelInfo? channelInfo = await helix.GetChannelInfoAsync(targetUser.Id, ct);
        string gameName = !string.IsNullOrEmpty(channelInfo?.GameName)
            ? channelInfo.GameName
            : "an unknown game";

        return $"Go check out {targetUser.DisplayName} at https://twitch.tv/{targetUser.Login} — they were last playing {gameName}!";
    }
}
