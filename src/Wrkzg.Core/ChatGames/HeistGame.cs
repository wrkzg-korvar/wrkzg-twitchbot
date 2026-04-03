using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.ChatGames;

/// <summary>
/// Heist game: players bet points and join a group heist.
/// After a join phase, each player individually succeeds or fails.
/// Winners get their bet * multiplier back.
/// </summary>
public class HeistGame : IChatGame
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;
    private readonly ILogger<HeistGame> _logger;
    private readonly GameMessageTemplates _msg;

    private HeistRound? _activeRound;
    private DateTimeOffset _lastHeistEnd = DateTimeOffset.MinValue;

    public string Trigger => "!heist";
    public string[] Aliases => Array.Empty<string>();
    public string Name => "Heist";
    public string Description => "Join a group heist! Bet points, survive, and earn big.";
    public bool IsEnabled { get; set; } = true;
    public int MinRolePriority { get; set; }

    private int _joinDuration = 60;
    private int _successRate = 55;
    private double _multiplier = 2.0;
    private int _minBet = 10;
    private int _maxBet = 10000;
    private int _cooldown = 300;
    private int _minPlayers = 1;

    private static readonly Dictionary<string, string> DefaultMessages = new()
    {
        ["Cooldown"] = "Heist is on cooldown! Try again in {remaining}s.",
        ["Usage"] = "Usage: !heist <amount> (min: {min}, max: {max})",
        ["BetRange"] = "Bet must be between {min} and {max} points.",
        ["NotEnoughPoints"] = "You don't have enough points!",
        ["Started"] = "{user} started a heist with {amount} points! Type !heist <amount> to join! ({duration}s)",
        ["AlreadyJoined"] = "You're already in the heist!",
        ["Joined"] = "{user} joined the heist with {amount} points! ({count} thieves)",
        ["NotEnoughPlayers"] = "Not enough players for the heist! (need {min}) Points refunded.",
        ["Begin"] = "The heist begins! {count} thieves are breaking in...",
        ["Success"] = "{user} escaped with {payout} points!",
        ["Failure"] = "{user} got caught! Lost {amount} points.",
        ["Result"] = "Result: {survivors}/{total} survived. Total payout: {payout} points.",
    };

    public HeistGame(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        ILogger<HeistGame> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _logger = logger;
        _msg = new GameMessageTemplates("Heist", DefaultMessages);
    }

    public async Task<string?> HandleAsync(ChatMessage message, CancellationToken ct = default)
    {
        await LoadSettingsAsync(ct);

        double secondsSinceLastHeist = (DateTimeOffset.UtcNow - _lastHeistEnd).TotalSeconds;
        if (secondsSinceLastHeist < _cooldown && _activeRound is null)
        {
            int remaining = (int)(_cooldown - secondsSinceLastHeist);
            return _msg.Get("Cooldown", ("remaining", remaining.ToString()));
        }

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

        if (_activeRound is null)
        {
            _activeRound = new HeistRound();
            _activeRound.Participants[message.UserId] = new HeistParticipant(message.DisplayName, bet, user.Id);

            user.Points -= bet;
            await users.UpdateAsync(user, ct);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_joinDuration * 1000, CancellationToken.None);
                    await ResolveHeistAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Heist timer error");
                }
            });

            return _msg.Get("Started",
                ("user", message.DisplayName),
                ("amount", bet.ToString()),
                ("duration", _joinDuration.ToString()));
        }

        if (_activeRound.Participants.ContainsKey(message.UserId))
        {
            return _msg.Get("AlreadyJoined");
        }

        _activeRound.Participants[message.UserId] = new HeistParticipant(message.DisplayName, bet, user.Id);

        user.Points -= bet;
        await users.UpdateAsync(user, ct);

        return _msg.Get("Joined",
            ("user", message.DisplayName),
            ("amount", bet.ToString()),
            ("count", _activeRound.Participants.Count.ToString()));
    }

    public Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Dictionary<string, string> GetMessageTemplates() => _msg.GetAll();
    public Dictionary<string, string> GetDefaultMessageTemplates() => _msg.GetDefaults();

    private async Task ResolveHeistAsync()
    {
        HeistRound? round = _activeRound;
        if (round is null || round.Participants.IsEmpty)
        {
            _activeRound = null;
            return;
        }

        if (round.Participants.Count < _minPlayers)
        {
            using IServiceScope refundScope = _scopeFactory.CreateScope();
            IUserRepository refundUsers = refundScope.ServiceProvider.GetRequiredService<IUserRepository>();
            foreach (HeistParticipant p in round.Participants.Values)
            {
                User? user = await refundUsers.GetByIdAsync(p.DbUserId);
                if (user is not null)
                {
                    user.Points += p.Bet;
                    await refundUsers.UpdateAsync(user);
                }
            }

            if (_chatClient.IsConnected)
            {
                await _chatClient.SendMessageAsync(
                    _msg.Get("NotEnoughPlayers", ("min", _minPlayers.ToString())));
            }
            _activeRound = null;
            _lastHeistEnd = DateTimeOffset.UtcNow;
            return;
        }

        Random rng = new();
        List<string> lines = new();
        lines.Add(_msg.Get("Begin", ("count", round.Participants.Count.ToString())));

        int survivors = 0;
        long totalPayout = 0;

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        foreach (HeistParticipant participant in round.Participants.Values)
        {
            bool success = rng.Next(100) < _successRate;
            if (success)
            {
                int payout = (int)(participant.Bet * _multiplier);
                User? user = await users.GetByIdAsync(participant.DbUserId);
                if (user is not null)
                {
                    user.Points += payout;
                    await users.UpdateAsync(user);
                }
                survivors++;
                totalPayout += payout;
                lines.Add(_msg.Get("Success",
                    ("user", participant.DisplayName),
                    ("payout", payout.ToString())));
            }
            else
            {
                lines.Add(_msg.Get("Failure",
                    ("user", participant.DisplayName),
                    ("amount", participant.Bet.ToString())));
            }
        }

        lines.Add(_msg.Get("Result",
            ("survivors", survivors.ToString()),
            ("total", round.Participants.Count.ToString()),
            ("payout", totalPayout.ToString())));

        if (_chatClient.IsConnected)
        {
            foreach (string line in lines)
            {
                await _chatClient.SendMessageAsync(line);
                await Task.Delay(500);
            }
        }

        _activeRound = null;
        _lastHeistEnd = DateTimeOffset.UtcNow;
    }

    private async Task LoadSettingsAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string? val = await settings.GetAsync("Games.Heist.JoinDuration", ct);
            if (val is not null && int.TryParse(val, out int jd)) { _joinDuration = jd; }

            val = await settings.GetAsync("Games.Heist.SuccessRate", ct);
            if (val is not null && int.TryParse(val, out int sr)) { _successRate = sr; }

            val = await settings.GetAsync("Games.Heist.Multiplier", ct);
            if (val is not null && double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double m)) { _multiplier = m; }

            val = await settings.GetAsync("Games.Heist.MinBet", ct);
            if (val is not null && int.TryParse(val, out int mn)) { _minBet = mn; }

            val = await settings.GetAsync("Games.Heist.MaxBet", ct);
            if (val is not null && int.TryParse(val, out int mx)) { _maxBet = mx; }

            val = await settings.GetAsync("Games.Heist.Cooldown", ct);
            if (val is not null && int.TryParse(val, out int cd)) { _cooldown = cd; }

            val = await settings.GetAsync("Games.Heist.MinPlayers", ct);
            if (val is not null && int.TryParse(val, out int mp)) { _minPlayers = mp; }

            val = await settings.GetAsync("Games.Heist.Enabled", ct);
            if (val is not null) { IsEnabled = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase); }

            val = await settings.GetAsync("Games.Heist.MinRolePriority", ct);
            if (val is not null && int.TryParse(val, out int rp)) { MinRolePriority = rp; }

            // Load message templates
            await _msg.LoadAsync(_scopeFactory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Heist settings");
        }
    }

    private sealed class HeistRound
    {
        public ConcurrentDictionary<string, HeistParticipant> Participants { get; } = new();
    }

    private sealed record HeistParticipant(string DisplayName, int Bet, int DbUserId);
}
