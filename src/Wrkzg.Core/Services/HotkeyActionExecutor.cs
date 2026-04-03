using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<HotkeyActionExecutor> _logger;

    public HotkeyActionExecutor(
        IServiceScopeFactory scopeFactory,
        ITwitchChatClient chatClient,
        IChatEventBroadcaster broadcaster,
        ILogger<HotkeyActionExecutor> logger)
    {
        _scopeFactory = scopeFactory;
        _chatClient = chatClient;
        _broadcaster = broadcaster;
        _logger = logger;
    }

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
