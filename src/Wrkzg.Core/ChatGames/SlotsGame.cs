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
/// Slots game: stateless — each spin is a single operation.
/// Three symbols are rolled. Matching symbols = payout.
/// </summary>
public class SlotsGame : IChatGame
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlotsGame> _logger;
    private readonly GameMessageTemplates _msg;

    private static readonly string[] DefaultSymbols = { "🍒", "🍋", "🍊", "💎", "7️⃣" };

    public string Trigger => "!slots";
    public string[] Aliases => new[] { "!slot" };
    public string Name => "Slots";
    public string Description => "Pull the lever! Match symbols to win big.";
    public bool IsEnabled { get; set; } = true;
    public int MinRolePriority { get; set; }

    private int _minBet = 10;
    private int _maxBet = 5000;

    private static readonly Dictionary<string, string> DefaultMessages = new()
    {
        ["Usage"] = "Usage: !slots <amount> (min: {min}, max: {max})",
        ["BetRange"] = "Bet must be between {min} and {max} points.",
        ["NotEnoughPoints"] = "You don't have enough points!",
        ["Jackpot"] = "🎰 [{s1} {s2} {s3}] — JACKPOT! {multiplier}x — +{payout} points!",
        ["TwoMatch"] = "🎰 [{s1} {s2} {s3}] — Two match! +{payout} points back.",
        ["NoMatch"] = "🎰 [{s1} {s2} {s3}] — No match. -{amount} points.",
    };

    public SlotsGame(IServiceScopeFactory scopeFactory, ILogger<SlotsGame> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _msg = new GameMessageTemplates("Slots", DefaultMessages);
    }

    public async Task<string?> HandleAsync(ChatMessage message, CancellationToken ct = default)
    {
        await LoadSettingsAsync(ct);

        string[] parts = message.Content.Split(' ', 2);
        if (parts.Length < 2 || !int.TryParse(parts[1], out int bet))
        {
            return _msg.Get("Usage", ("min", _minBet.ToString()), ("max", _maxBet.ToString()));
        }

        if (bet < _minBet || bet > _maxBet)
        {
            return _msg.Get("BetRange", ("min", _minBet.ToString()), ("max", _maxBet.ToString()));
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        User? user = await users.GetByTwitchIdAsync(message.UserId, ct);

        if (user is null || user.Points < bet)
        {
            return _msg.Get("NotEnoughPoints");
        }

        user.Points -= bet;

        Random rng = new();
        string s1 = DefaultSymbols[rng.Next(DefaultSymbols.Length)];
        string s2 = DefaultSymbols[rng.Next(DefaultSymbols.Length)];
        string s3 = DefaultSymbols[rng.Next(DefaultSymbols.Length)];

        int payout = 0;
        string templateKey;

        if (s1 == s2 && s2 == s3)
        {
            int multiplier = s1 == "7️⃣" ? 50 : s1 == "💎" ? 10 : 5;
            payout = bet * multiplier;
            templateKey = "Jackpot";
            user.Points += payout;
            await users.UpdateAsync(user, ct);
            return _msg.Get(templateKey,
                ("s1", s1), ("s2", s2), ("s3", s3),
                ("multiplier", multiplier.ToString()),
                ("payout", payout.ToString()));
        }

        if (s1 == s2 || s2 == s3 || s1 == s3)
        {
            payout = bet / 2;
            user.Points += payout;
            await users.UpdateAsync(user, ct);
            return _msg.Get("TwoMatch",
                ("s1", s1), ("s2", s2), ("s3", s3),
                ("payout", payout.ToString()));
        }

        await users.UpdateAsync(user, ct);
        return _msg.Get("NoMatch",
            ("s1", s1), ("s2", s2), ("s3", s3),
            ("amount", bet.ToString()));
    }

    public Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Dictionary<string, string> GetMessageTemplates() => _msg.GetAll();
    public Dictionary<string, string> GetDefaultMessageTemplates() => _msg.GetDefaults();

    private async Task LoadSettingsAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string? val = await settings.GetAsync("Games.Slots.MinBet", ct);
            if (val is not null && int.TryParse(val, out int mn)) { _minBet = mn; }

            val = await settings.GetAsync("Games.Slots.MaxBet", ct);
            if (val is not null && int.TryParse(val, out int mx)) { _maxBet = mx; }

            val = await settings.GetAsync("Games.Slots.Enabled", ct);
            if (val is not null) { IsEnabled = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase); }

            await _msg.LoadAsync(_scopeFactory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Slots settings");
        }
    }
}
