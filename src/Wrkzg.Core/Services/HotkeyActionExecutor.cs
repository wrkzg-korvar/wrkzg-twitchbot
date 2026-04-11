using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Effects;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Services;

/// <summary>
/// Executes the action associated with a hotkey binding.
/// Called by HotkeyListenerService when a registered hotkey is pressed.
/// </summary>
public class HotkeyActionExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwitchChatClient _chatClient;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly EffectEngine _effectEngine;
    private readonly SongRequestService _songRequestService;
    private readonly ILogger<HotkeyActionExecutor> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of <see cref="HotkeyActionExecutor"/> with the required dependencies.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes to resolve scoped services.</param>
    /// <param name="chatClient">The Twitch IRC chat client for sending chat messages.</param>
    /// <param name="broadcaster">Broadcasts real-time events to dashboard and overlays.</param>
    /// <param name="effectEngine">The automation engine for running effect chains.</param>
    /// <param name="songRequestService">Song request queue management.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public HotkeyActionExecutor(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        IChatEventBroadcaster broadcaster,
        EffectEngine effectEngine,
        SongRequestService songRequestService,
        ILogger<HotkeyActionExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _broadcaster = broadcaster;
        _effectEngine = effectEngine;
        _songRequestService = songRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the action defined by the given hotkey binding (e.g., send chat message, modify counter).
    /// </summary>
    /// <param name="binding">The hotkey binding containing the action type and payload.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ExecuteAsync(HotkeyBinding binding, CancellationToken ct = default)
    {
        try
        {
            switch (binding.ActionType)
            {
                case "ChatMessage":
                    if (_chatClient.IsConnected)
                    {
                        await _chatClient.SendMessageAsync(binding.ActionPayload, ct);
                    }
                    break;

                case "CounterIncrement":
                case "CounterDecrement":
                case "CounterReset":
                    if (int.TryParse(binding.ActionPayload, out int counterId))
                    {
                        using IServiceScope scope = _scopeFactory.CreateScope();
                        ICounterRepository counters = scope.ServiceProvider.GetRequiredService<ICounterRepository>();
                        Counter? counter = await counters.GetByIdAsync(counterId, ct);
                        if (counter is not null)
                        {
                            if (binding.ActionType == "CounterIncrement")
                            {
                                counter.Value++;
                            }
                            else if (binding.ActionType == "CounterDecrement")
                            {
                                counter.Value--;
                            }
                            else
                            {
                                counter.Value = 0;
                            }
                            await counters.UpdateAsync(counter, ct);
                            await _broadcaster.BroadcastCounterUpdatedAsync(
                                counter.Id, counter.Name, counter.Value, ct);
                        }
                    }
                    break;

                case "RunEffect":
                    if (int.TryParse(binding.ActionPayload, out int effectListId))
                    {
                        EffectTriggerContext triggerContext = new()
                        {
                            EventType = "hotkey",
                            Username = "Hotkey",
                            Data = new Dictionary<string, string>
                            {
                                { "hotkey", binding.KeyCombination },
                                { "binding_id", binding.Id.ToString() },
                                { "description", binding.Description ?? "" }
                            }
                        };
                        await _effectEngine.ExecuteSingleAsync(effectListId, triggerContext, ct);
                    }
                    break;

                case "PollStart":
                    try
                    {
                        using (IServiceScope pollStartScope = _scopeFactory.CreateScope())
                        {
                            PollService pollService = pollStartScope.ServiceProvider.GetRequiredService<PollService>();
                            HotkeyPollStartPayload? pollConfig = JsonSerializer.Deserialize<HotkeyPollStartPayload>(
                                binding.ActionPayload, _jsonOptions);
                            if (pollConfig?.Question is not null && pollConfig.Options is { Length: >= 2 })
                            {
                                PollResult result = await pollService.CreateBotPollAsync(
                                    pollConfig.Question, pollConfig.Options, pollConfig.DurationSeconds, "Hotkey", ct);
                                if (!result.Success)
                                {
                                    _logger.LogWarning("Hotkey PollStart failed: {Error}", result.Error);
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Invalid PollStart payload: {Payload}", binding.ActionPayload);
                    }
                    break;

                case "PollEnd":
                    using (IServiceScope pollEndScope = _scopeFactory.CreateScope())
                    {
                        PollService pollEndService = pollEndScope.ServiceProvider.GetRequiredService<PollService>();
                        PollResult endResult = await pollEndService.EndPollAsync(PollEndReason.ManuallyClosed, ct);
                        if (!endResult.Success)
                        {
                            _logger.LogInformation("Hotkey PollEnd: {Message}", endResult.Error);
                        }
                    }
                    break;

                case "RaffleStart":
                    try
                    {
                        using (IServiceScope raffleStartScope = _scopeFactory.CreateScope())
                        {
                            RaffleService raffleService = raffleStartScope.ServiceProvider.GetRequiredService<RaffleService>();
                            HotkeyRaffleStartPayload? raffleConfig = JsonSerializer.Deserialize<HotkeyRaffleStartPayload>(
                                binding.ActionPayload, _jsonOptions);
                            if (raffleConfig?.Title is not null)
                            {
                                RaffleResult result = await raffleService.CreateAsync(
                                    raffleConfig.Title,
                                    raffleConfig.Keyword ?? "!join",
                                    raffleConfig.DurationSeconds,
                                    raffleConfig.MaxEntries,
                                    "Hotkey", ct);
                                if (!result.Success)
                                {
                                    _logger.LogWarning("Hotkey RaffleStart failed: {Error}", result.Error);
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Invalid RaffleStart payload: {Payload}", binding.ActionPayload);
                    }
                    break;

                case "SongSkip":
                    string skipResult = await _songRequestService.SkipCurrentAsync(ct);
                    if (_chatClient.IsConnected && !string.IsNullOrWhiteSpace(skipResult))
                    {
                        await _chatClient.SendMessageAsync(skipResult, ct);
                    }
                    break;

                case "PlayAlert":
                    string alertMessage = binding.ActionPayload;
                    if (!string.IsNullOrWhiteSpace(alertMessage))
                    {
                        await _broadcaster.BroadcastFollowEventAsync(alertMessage, ct);
                    }
                    break;

                case "ObsSceneSwitch":
                {
                    IObsWebSocketService obs = _scopeFactory.CreateScope().ServiceProvider
                        .GetRequiredService<IObsWebSocketService>();
                    if (obs.IsConnected)
                    {
                        await obs.SwitchSceneAsync(binding.ActionPayload, ct);
                    }
                    else
                    {
                        _logger.LogWarning("OBS not connected — cannot switch scene");
                    }
                    break;
                }

                case "ObsSourceToggle":
                {
                    IObsWebSocketService obs = _scopeFactory.CreateScope().ServiceProvider
                        .GetRequiredService<IObsWebSocketService>();
                    if (obs.IsConnected)
                    {
                        // Payload format: "SceneName|SourceName" or "SceneName|SourceName|true/false"
                        string[] parts = binding.ActionPayload.Split('|', 3);
                        if (parts.Length >= 2)
                        {
                            string scene = parts[0];
                            string source = parts[1];
                            bool? forceVisible = parts.Length >= 3 && bool.TryParse(parts[2], out bool v)
                                ? v
                                : null;

                            if (forceVisible.HasValue)
                            {
                                await obs.SetSourceVisibilityAsync(scene, source, forceVisible.Value, ct);
                            }
                            else
                            {
                                // Toggle: get current visibility, then invert
                                IReadOnlyList<ObsSourceInfo> sources = await obs.GetSourcesAsync(scene, ct);
                                ObsSourceInfo? s = sources.FirstOrDefault(x =>
                                    string.Equals(x.SourceName, source, StringComparison.OrdinalIgnoreCase));
                                if (s is not null)
                                {
                                    await obs.SetSourceVisibilityAsync(scene, source, !s.IsVisible, ct);
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("OBS not connected — cannot toggle source");
                    }
                    break;
                }

                default:
                    _logger.LogWarning("Unknown hotkey action type: {ActionType}", binding.ActionType);
                    break;
            }

            _logger.LogInformation("Hotkey executed: {Description} ({KeyCombination})",
                binding.Description ?? binding.ActionType, binding.KeyCombination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute hotkey action: {ActionType}", binding.ActionType);
        }
    }
}

/// <summary>JSON payload for PollStart hotkey action.</summary>
internal sealed record HotkeyPollStartPayload(string Question, string[] Options, int DurationSeconds = 60);

/// <summary>JSON payload for RaffleStart hotkey action.</summary>
internal sealed record HotkeyRaffleStartPayload(string Title, string? Keyword, int? DurationSeconds, int? MaxEntries);
