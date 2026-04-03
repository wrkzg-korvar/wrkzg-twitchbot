using System;
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
/// Trivia game: bot asks a question, first correct answer wins points.
/// Uses HandleActiveRoundMessageAsync to check every chat message against the answer.
/// </summary>
public class TriviaGame : IChatGame
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;
    private readonly ILogger<TriviaGame> _logger;
    private readonly GameMessageTemplates _msg;

    private TriviaRound? _activeRound;
    private DateTimeOffset _lastRoundEnd = DateTimeOffset.MinValue;

    public string Trigger => "!trivia";
    public string[] Aliases => Array.Empty<string>();
    public string Name => "Trivia";
    public string Description => "Answer trivia questions to win points!";
    public bool IsEnabled { get; set; } = true;
    public int MinRolePriority { get; set; }

    private int _answerDuration = 30;
    private int _reward = 50;
    private int _cooldown = 30;

    private static readonly Dictionary<string, string> DefaultMessages = new()
    {
        ["Active"] = "A trivia question is already active! Answer in chat.",
        ["Cooldown"] = "Trivia is on cooldown! Try again in {remaining}s.",
        ["NoQuestions"] = "No trivia questions available! Add some in the dashboard.",
        ["Question"] = "Trivia: {category}{question} ({duration}s)",
        ["TimesUp"] = "Time's up! The answer was: {answer}",
        ["Correct"] = "{user} got it right! The answer was: {answer}. +{reward} points!",
    };

    public TriviaGame(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        ILogger<TriviaGame> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _logger = logger;
        _msg = new GameMessageTemplates("Trivia", DefaultMessages);
    }

    public Dictionary<string, string> GetMessageTemplates() => _msg.GetAll();
    public Dictionary<string, string> GetDefaultMessageTemplates() => _msg.GetDefaults();

    public async Task<string?> HandleAsync(ChatMessage message, CancellationToken ct = default)
    {
        await LoadSettingsAsync(ct);

        if (_activeRound is not null)
        {
            return _msg.Get("Active");
        }

        double secondsSinceLast = (DateTimeOffset.UtcNow - _lastRoundEnd).TotalSeconds;
        if (secondsSinceLast < _cooldown)
        {
            int remaining = (int)(_cooldown - secondsSinceLast);
            return _msg.Get("Cooldown", ("remaining", remaining.ToString()));
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        ITriviaQuestionRepository repo = scope.ServiceProvider.GetRequiredService<ITriviaQuestionRepository>();
        TriviaQuestion? question = await repo.GetRandomAsync(ct);

        if (question is null)
        {
            return _msg.Get("NoQuestions");
        }

        _activeRound = new TriviaRound(question.Answer, question.AcceptedAnswers.ToArray());

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_answerDuration * 1000, CancellationToken.None);
                if (_activeRound is not null)
                {
                    string answer = _activeRound.CorrectAnswer;
                    _activeRound = null;
                    _lastRoundEnd = DateTimeOffset.UtcNow;
                    if (_chatClient.IsConnected)
                    {
                        await _chatClient.SendMessageAsync(
                            _msg.Get("TimesUp", ("answer", answer)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trivia timeout error");
            }
        });

        string categoryTag = question.Category is not null ? $"[{question.Category}] " : "";
        return _msg.Get("Question",
            ("category", categoryTag),
            ("question", question.Question),
            ("duration", _answerDuration.ToString()));
    }

    public async Task<bool> HandleActiveRoundMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        if (_activeRound is null)
        {
            return false;
        }

        string answer = message.Content.Trim();

        bool isCorrect = string.Equals(answer, _activeRound.CorrectAnswer, StringComparison.OrdinalIgnoreCase)
            || _activeRound.AcceptedAlternatives.Any(a =>
                string.Equals(answer, a, StringComparison.OrdinalIgnoreCase));

        if (!isCorrect)
        {
            return false;
        }

        string correctAnswer = _activeRound.CorrectAnswer;
        _activeRound = null;
        _lastRoundEnd = DateTimeOffset.UtcNow;

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            User? user = await users.GetByTwitchIdAsync(message.UserId, ct);
            if (user is not null)
            {
                user.Points += _reward;
                await users.UpdateAsync(user, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to award trivia points to {User}", message.Username);
        }

        if (_chatClient.IsConnected)
        {
            await _chatClient.SendMessageAsync(
                _msg.Get("Correct",
                    ("user", message.DisplayName),
                    ("answer", correctAnswer),
                    ("reward", _reward.ToString())));
        }

        return true;
    }

    private async Task LoadSettingsAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string? val = await settings.GetAsync("Games.Trivia.AnswerDuration", ct);
            if (val is not null && int.TryParse(val, out int ad)) { _answerDuration = ad; }

            val = await settings.GetAsync("Games.Trivia.Reward", ct);
            if (val is not null && int.TryParse(val, out int rw)) { _reward = rw; }

            val = await settings.GetAsync("Games.Trivia.Cooldown", ct);
            if (val is not null && int.TryParse(val, out int cd)) { _cooldown = cd; }

            val = await settings.GetAsync("Games.Trivia.Enabled", ct);
            if (val is not null) { IsEnabled = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase); }

            await _msg.LoadAsync(_scopeFactory, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Trivia settings");
        }
    }

    private sealed record TriviaRound(string CorrectAnswer, string[] AcceptedAlternatives);
}
