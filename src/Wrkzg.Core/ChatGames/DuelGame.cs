using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.ChatGames;

/// <summary>
/// Duel game: Player A challenges Player B for a bet amount.
/// B must !accept within a timeout. 50/50 chance.
/// </summary>
public class DuelGame : IChatGame
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;
    private readonly ILogger<DuelGame> _logger;
    private readonly GameMessageTemplates _msg;

    private DuelChallenge? _pendingDuel;
    private DateTimeOffset _lastDuelEnd = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public string Trigger => "!duel";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public string Name => "Duel";

    /// <inheritdoc />
    public string Description => "Challenge another viewer to a 1v1 points duel!";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public int MinRolePriority { get; set; }

    private int _acceptTimeout = 60;
    private int _minBet = 10;
    private int _maxBet = 10000;
    private int _cooldown = 60;

    private static readonly Dictionary<string, string> DefaultMessages = new()
    {
        ["Cooldown"] = "Duel is on cooldown! Try again in {remaining}s.",
        ["Pending"] = "A duel is already pending! Wait for it to resolve.",
        ["Usage"] = "Usage: !duel @username <amount>",
        ["BetRange"] = "Bet must be between {min} and {max} points.",
        ["SelfDuel"] = "You can't duel yourself!",
        ["NotEnoughPoints"] = "You don't have enough points!",
        ["Challenge"] = "{challenger} challenges @{target} to a duel for {amount} points! Type !accept in {timeout}s.",
        ["Expired"] = "{target} didn't accept the duel. Challenge expired.",
        ["TargetBroke"] = "{target} doesn't have enough points! Duel cancelled.",
        ["Cancelled"] = "Duel cancelled — couldn't find both players.",
        ["Fight"] = "{challenger} vs {target} — {amount} points on the line...",
        ["Winner"] = "{winner} wins the duel! +{amount} points.",
    };

    /// <summary>
    /// Initializes a new instance of <see cref="DuelGame"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="chatClient">The Twitch IRC chat client for sending game messages.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public DuelGame(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        ILogger<DuelGame> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _logger = logger;
        _msg = new GameMessageTemplates("Duel", DefaultMessages);
    }

    /// <inheritdoc />
    public async Task<string?> HandleAsync(ChatMessage message, CancellationToken ct = default)
    {
        await LoadSettingsAsync(ct);

        double secondsSinceLast = (DateTimeOffset.UtcNow - _lastDuelEnd).TotalSeconds;
        if (secondsSinceLast < _cooldown && _pendingDuel is null)
        {
            int remaining = (int)(_cooldown - secondsSinceLast);
            return _msg.Get("Cooldown", ("remaining", remaining.ToString()));
        }

        if (_pendingDuel is not null)
        {
            return _msg.Get("Pending");
        }

        string[] parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return _msg.Get("Usage");
        }

        string targetName = parts[1].TrimStart('@').ToLowerInvariant();
        if (!int.TryParse(parts[2], out int bet) || bet < _minBet || bet > _maxBet)
        {
            return _msg.Get("BetRange", ("min", _minBet.ToString()), ("max", _maxBet.ToString()));
        }

        if (string.Equals(targetName, message.Username, StringComparison.OrdinalIgnoreCase))
        {
            return _msg.Get("SelfDuel");
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User? challenger = await users.GetByTwitchIdAsync(message.UserId, ct);
        if (challenger is null || challenger.Points < bet)
        {
            return _msg.Get("NotEnoughPoints");
        }

        _pendingDuel = new DuelChallenge(
            message.UserId, message.DisplayName, challenger.Id,
            targetName, bet, DateTimeOffset.UtcNow);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_acceptTimeout * 1000, CancellationToken.None);
                if (_pendingDuel is not null && _pendingDuel.ChallengerTwitchId == message.UserId)
                {
                    _pendingDuel = null;
                    if (_chatClient.IsConnected)
                    {
                        await _chatClient.SendMessageAsync(
                            _msg.Get("Expired", ("target", targetName)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Duel timeout error");
            }
        });

        return _msg.Get("Challenge",
            ("challenger", message.DisplayName),
            ("target", targetName),
            ("amount", bet.ToString()),
            ("timeout", _acceptTimeout.ToString()));
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetMessageTemplates() => _msg.GetAll();

    /// <inheritdoc />
    public Dictionary<string, string> GetDefaultMessageTemplates() => _msg.GetDefaults();

    /// <inheritdoc />
    public async Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (_pendingDuel is null)
        {
            return false;
        }

        string content = message.Content.Trim().ToLowerInvariant();
        if (content != "!accept")
        {
            return false;
        }

        if (!string.Equals(message.Username, _pendingDuel.TargetUsername, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        DuelChallenge duel = _pendingDuel;
        _pendingDuel = null;

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        User? challengerUser = await users.GetByIdAsync(duel.ChallengerDbId, ct);
        User? target = await users.GetByTwitchIdAsync(message.UserId, ct);

        if (challengerUser is null || target is null)
        {
            if (_chatClient.IsConnected)
            {
                await _chatClient.SendMessageAsync(_msg.Get("Cancelled"));
            }
            _lastDuelEnd = DateTimeOffset.UtcNow;
            return true;
        }

        if (target.Points < duel.Bet)
        {
            if (_chatClient.IsConnected)
            {
                await _chatClient.SendMessageAsync(
                    _msg.Get("TargetBroke", ("target", message.DisplayName)));
            }
            _lastDuelEnd = DateTimeOffset.UtcNow;
            return true;
        }

        Random rng = new();
        bool challengerWins = rng.Next(2) == 0;

        if (challengerWins)
        {
            challengerUser.Points += duel.Bet;
            target.Points -= duel.Bet;
        }
        else
        {
            challengerUser.Points -= duel.Bet;
            target.Points += duel.Bet;
        }

        await users.UpdateAsync(challengerUser, ct);
        await users.UpdateAsync(target, ct);

        string winner = challengerWins ? duel.ChallengerDisplayName : message.DisplayName;

        if (_chatClient.IsConnected)
        {
            await _chatClient.SendMessageAsync(
                _msg.Get("Fight",
                    ("challenger", duel.ChallengerDisplayName),
                    ("target", message.DisplayName),
                    ("amount", duel.Bet.ToString())));
            await Task.Delay(1500, ct);
            await _chatClient.SendMessageAsync(
                _msg.Get("Winner",
                    ("winner", winner),
                    ("amount", duel.Bet.ToString())));
        }

        _lastDuelEnd = DateTimeOffset.UtcNow;
        return true;
    }

    private async Task LoadSettingsAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string? val = await settings.GetAsync("Games.Duel.AcceptTimeout", ct);
            if (val is not null && int.TryParse(val, out int at)) { _acceptTimeout = at; }

            val = await settings.GetAsync("Games.Duel.MinBet", ct);
            if (val is not null && int.TryParse(val, out int mn)) { _minBet = mn; }

            val = await settings.GetAsync("Games.Duel.MaxBet", ct);
            if (val is not null && int.TryParse(val, out int mx)) { _maxBet = mx; }

            val = await settings.GetAsync("Games.Duel.Cooldown", ct);
            if (val is not null && int.TryParse(val, out int cd)) { _cooldown = cd; }

            val = await settings.GetAsync("Games.Duel.Enabled", ct);
            if (val is not null) { IsEnabled = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase); }

            await _msg.LoadAsync(_scopeFactory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Duel settings");
        }
    }

    private sealed record DuelChallenge(
        string ChallengerTwitchId, string ChallengerDisplayName, int ChallengerDbId,
        string TargetUsername, int Bet, DateTimeOffset CreatedAt);
}
