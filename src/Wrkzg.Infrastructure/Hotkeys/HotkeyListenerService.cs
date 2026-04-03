using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Infrastructure.Hotkeys;

/// <summary>
/// Hosted service that manages the lifecycle of global hotkey bindings.
/// Loads bindings from the database, registers them with the platform listener,
/// and dispatches actions when hotkeys are pressed.
/// </summary>
public class HotkeyListenerService : IHostedService
{
    private readonly IHotkeyListener _listener;
    private readonly HotkeyActionExecutor _executor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HotkeyListenerService> _logger;
    private readonly Dictionary<int, HotkeyBinding> _bindings = new();

    public HotkeyListenerService(
        IHotkeyListener listener,
        HotkeyActionExecutor executor,
        IServiceScopeFactory scopeFactory,
        ILogger<HotkeyListenerService> logger)
    {
        _listener = listener;
        _executor = executor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("HotkeyListenerService starting");

        _listener.OnHotkeyPressed += OnHotkeyPressed;
        await _listener.StartListeningAsync(ct);
        await RefreshBindingsAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("HotkeyListenerService stopping");

        _listener.OnHotkeyPressed -= OnHotkeyPressed;
        await _listener.StopListeningAsync(ct);
    }

    /// <summary>
    /// Reloads all bindings from the database and re-registers hotkeys.
    /// Called on startup and after adding/removing/editing bindings.
    /// </summary>
    public async Task RefreshBindingsAsync(CancellationToken ct = default)
    {
        _listener.UnregisterAll();
        _bindings.Clear();

        using IServiceScope scope = _scopeFactory.CreateScope();
        IHotkeyBindingRepository repo = scope.ServiceProvider.GetRequiredService<IHotkeyBindingRepository>();
        IReadOnlyList<HotkeyBinding> enabled = await repo.GetEnabledAsync(ct);

        foreach (HotkeyBinding binding in enabled)
        {
            bool registered = _listener.RegisterHotkey(binding.Id, binding.KeyCombination);
            if (registered)
            {
                _bindings[binding.Id] = binding;
            }
        }

        _logger.LogInformation("Registered {Count} hotkey bindings", _bindings.Count);
    }

    /// <summary>Triggers a hotkey action by binding ID (for API/Stream Deck).</summary>
    public async Task TriggerByIdAsync(int bindingId, CancellationToken ct = default)
    {
        if (_bindings.TryGetValue(bindingId, out HotkeyBinding? binding))
        {
            await _executor.ExecuteAsync(binding, ct);
        }
        else
        {
            // Binding might not be cached — try loading from DB
            using IServiceScope scope = _scopeFactory.CreateScope();
            IHotkeyBindingRepository repo = scope.ServiceProvider.GetRequiredService<IHotkeyBindingRepository>();
            HotkeyBinding? dbBinding = await repo.GetByIdAsync(bindingId, ct);
            if (dbBinding is not null && dbBinding.IsEnabled)
            {
                await _executor.ExecuteAsync(dbBinding, ct);
            }
        }
    }

    private async void OnHotkeyPressed(int bindingId)
    {
        try
        {
            await TriggerByIdAsync(bindingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling hotkey press for binding {Id}", bindingId);
        }
    }
}
