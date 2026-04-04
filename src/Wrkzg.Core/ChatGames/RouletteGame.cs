using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.ChatGames;

/// <summary>
/// Roulette game: players bet on red/black. After a collect phase,
/// the wheel spins and winners get 2x their bet.
/// </summary>
public class RouletteGame : IChatGame
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;
    private readonly ILogger<RouletteGame> _logger;
    private readonly GameMessageTemplates _msg;

    private RouletteRound? _activeRound;
    private DateTimeOffset _lastRoundEnd = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public string Trigger => "!roulette";

    /// <inheritdoc />
    public string[] Aliases => new[] { "!rl" };

    /// <inheritdoc />
    public string Name => "Roulette";

    /// <inheritdoc />
    public string Description => "Bet on red or black! 2x payout on a match.";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public int MinRolePriority { get; set; }

    private int _spinDuration = 30;
    private int _minBet = 10;
    private int _maxBet = 5000;
    private int _cooldown = 60;

    private static readonly string[] RedNumbers = { "1", "3", "5", "7", "9", "12", "14", "16", "18", "19", "21", "23", "25", "27", "30", "32", "34", "36" };

    private static readonly Dictionary<string, string> DefaultMessages = new()
    {
        ["Cooldown"] = "Roulette is on cooldown! Try again in {remaining}s.",
        ["Usage"] = "Usage: !roulette <amount> <red|black>",
        ["BetRange"] = "Bet must be between {min} and {max} points.",
        ["InvalidColor"] = "Choose red or black! Usage: !roulette <amount> <red|black>",
        ["NotEnoughPoints"] = "You don't have enough points!",
        ["Started"] = "{user} bets {amount} on {color}! Place your bets! ({duration}s)",
        ["Bet"] = "{user} bets {amount} on {color}!",
        ["Spin"] = "The wheel spins... {emoji} {number} {color}!",
        ["Win"] = "{user} wins {payout} points!",
        ["Lose"] = "{user} loses {amount} points.",
    };

    /// <summary>
    /// Initializes a new instance of <see cref="RouletteGame"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped repositories.</param>
    /// <param name="chatClient">The Twitch IRC chat client for sending game messages.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public RouletteGame(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        ILogger<RouletteGame> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _logger = logger;
        _msg = new GameMessageTemplates("Roulette", DefaultMessages);
    }

    /// <inheritdoc />
    public async Task<string?> HandleAsync(ChatMessage message, CancellationToken ct = default)
    {
        await LoadSettingsAsync(ct);

        double secondsSinceLast = (DateTimeOffset.UtcNow - _lastRoundEnd).TotalSeconds;
        if (secondsSinceLast < _cooldown && _activeRound is null)
        {
            int remaining = (int)(_cooldown - secondsSinceLast);
            return _msg.Get("Cooldown", ("remaining", remaining.ToString()));
        }

        string[] parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return _msg.Get("Usage");
        }

        if (!int.TryParse(parts[1], out int bet) || bet < _minBet || bet > _maxBet)
        {
            return _msg.Get("BetRange", ("min", _minBet.ToString()), ("max", _maxBet.ToString()));
        }

        string color = parts[2].ToLowerInvariant();
        if (color != "red" && color != "black")
        {
            return _msg.Get("InvalidColor");
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User? user = await users.GetByTwitchIdAsync(message.UserId, ct);

        if (user is null || user.Points < bet)
        {
            return _msg.Get("NotEnoughPoints");
        }

        user.Points -= bet;
        await users.UpdateAsync(user, ct);

        if (_activeRound is null)
        {
            _activeRound = new RouletteRound();
            _activeRound.Bets[message.UserId] = new RouletteBet(message.DisplayName, bet, color, user.Id);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_spinDuration * 1000, CancellationToken.None);
                    await ResolveRouletteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Roulette timer error");
                }
            });

            return _msg.Get("Started",
                ("user", message.DisplayName),
                ("amount", bet.ToString()),
                ("color", color),
                ("duration", _spinDuration.ToString()));
        }

        if (_activeRound.Bets.TryGetValue(message.UserId, out RouletteBet? oldBet))
        {
            User? refundUser = await users.GetByIdAsync(oldBet.DbUserId, ct);
            if (refundUser is not null)
            {
                refundUser.Points += oldBet.Amount;
                await users.UpdateAsync(refundUser, ct);
            }
        }

        _activeRound.Bets[message.UserId] = new RouletteBet(message.DisplayName, bet, color, user.Id);
        return _msg.Get("Bet",
            ("user", message.DisplayName),
            ("amount", bet.ToString()),
            ("color", color));
    }

    /// <inheritdoc />
    public Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetMessageTemplates() => _msg.GetAll();

    /// <inheritdoc />
    public Dictionary<string, string> GetDefaultMessageTemplates() => _msg.GetDefaults();

    private async Task ResolveRouletteAsync()
    {
        RouletteRound? round = _activeRound;
        if (round is null || round.Bets.IsEmpty)
        {
            _activeRound = null;
            return;
        }

        Random rng = new();
        int number = rng.Next(0, 37);
        bool isRed = Array.Exists(RedNumbers, n => n == number.ToString(System.Globalization.CultureInfo.InvariantCulture));
        bool isGreen = number == 0;
        string winningColor = isGreen ? "green" : isRed ? "red" : "black";
        string colorEmoji = isGreen ? "🟢" : isRed ? "🔴" : "⚫";

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        List<string> lines = new();
        lines.Add(_msg.Get("Spin",
            ("emoji", colorEmoji),
            ("number", number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("color", winningColor.ToUpperInvariant())));

        foreach (RouletteBet bet in round.Bets.Values)
        {
            if (!isGreen && string.Equals(bet.Color, winningColor, StringComparison.OrdinalIgnoreCase))
            {
                int payout = bet.Amount * 2;
                User? winUser = await users.GetByIdAsync(bet.DbUserId);
                if (winUser is not null)
                {
                    winUser.Points += payout;
                    await users.UpdateAsync(winUser);
                }
                lines.Add(_msg.Get("Win",
                    ("user", bet.DisplayName),
                    ("payout", payout.ToString())));
            }
            else
            {
                lines.Add(_msg.Get("Lose",
                    ("user", bet.DisplayName),
                    ("amount", bet.Amount.ToString())));
            }
        }

        if (_chatClient.IsConnected)
        {
            foreach (string line in lines)
            {
                await _chatClient.SendMessageAsync(line);
                await Task.Delay(500);
            }
        }

        _activeRound = null;
        _lastRoundEnd = DateTimeOffset.UtcNow;
    }

    private async Task LoadSettingsAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string? val = await settings.GetAsync("Games.Roulette.SpinDuration", ct);
            if (val is not null && int.TryParse(val, out int sd)) { _spinDuration = sd; }

            val = await settings.GetAsync("Games.Roulette.MinBet", ct);
            if (val is not null && int.TryParse(val, out int mn)) { _minBet = mn; }

            val = await settings.GetAsync("Games.Roulette.MaxBet", ct);
            if (val is not null && int.TryParse(val, out int mx)) { _maxBet = mx; }

            val = await settings.GetAsync("Games.Roulette.Cooldown", ct);
            if (val is not null && int.TryParse(val, out int cd)) { _cooldown = cd; }

            val = await settings.GetAsync("Games.Roulette.Enabled", ct);
            if (val is not null) { IsEnabled = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase); }

            await _msg.LoadAsync(_scopeFactory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Roulette settings");
        }
    }

    private sealed class RouletteRound
    {
        public ConcurrentDictionary<string, RouletteBet> Bets { get; } = new();
    }

    private sealed record RouletteBet(string DisplayName, int Amount, string Color, int DbUserId);
}
