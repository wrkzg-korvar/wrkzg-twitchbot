using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Services;

/// <summary>
/// Manages all registered chat games. Integrated into ChatMessagePipeline
/// AFTER the CommandProcessor (games have lowest priority).
/// </summary>
public class ChatGameManager
{
    private readonly IReadOnlyList<IChatGame> _games;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;
    private readonly ILogger<ChatGameManager> _logger;

    public ChatGameManager(
        IEnumerable<IChatGame> games,
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        ILogger<ChatGameManager> logger)
    {
        _games = games.ToList();
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Returns all registered games for the dashboard API.
    /// </summary>
    public IReadOnlyList<IChatGame> GetAllGames() => _games;

    /// <summary>
    /// Checks if any game has an active round that wants to consume the message
    /// (e.g. trivia answers, duel !accept). Called BEFORE game trigger matching.
    /// </summary>
    public async Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        foreach (IChatGame game in _games)
        {
            if (!game.IsEnabled)
            {
                continue;
            }

            try
            {
                if (await game.HandleActiveRoundMessageAsync(message, ct))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in {Game}.HandleActiveRoundMessageAsync", game.Name);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the message matches a game trigger and handles it.
    /// </summary>
    public async Task<bool> HandleMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (!message.Content.StartsWith('!'))
        {
            return false;
        }

        string trigger = message.Content.Split(' ', 2)[0].ToLowerInvariant();

        foreach (IChatGame game in _games)
        {
            if (!game.IsEnabled)
            {
                continue;
            }

            if (!string.Equals(game.Trigger, trigger, StringComparison.OrdinalIgnoreCase)
                && !game.Aliases.Any(a => string.Equals(a, trigger, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Role check (uses v1.5.0 Roles system)
            if (game.MinRolePriority > 0)
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IRoleRepository roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
                    IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                    User? user = await users.GetByTwitchIdAsync(message.UserId, ct);
                    if (user is not null)
                    {
                        int userPriority = await roles.GetHighestPriorityForUserAsync(user.Id, ct);
                        if (userPriority < game.MinRolePriority)
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Role check failed for game {Game}", game.Name);
                }
            }

            try
            {
                string? response = await game.HandleAsync(message, ct);
                if (response is not null && _chatClient.IsConnected)
                {
                    await _chatClient.SendMessageAsync(response, ct);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in {Game}.HandleAsync", game.Name);
                return true;
            }
        }

        return false;
    }
}
