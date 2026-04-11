using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Core.Effects.EffectTypes;

/// <summary>Switches the active OBS scene.</summary>
public class ObsSceneSwitchEffect : IEffectType
{
    private readonly IObsWebSocketService _obs;
    private readonly ILogger<ObsSceneSwitchEffect> _logger;

    /// <inheritdoc />
    public string Id => "obs.scene_switch";

    /// <inheritdoc />
    public string DisplayName => "OBS: Scene wechseln";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "scene_name" };

    /// <summary>
    /// Initializes a new instance of the <see cref="ObsSceneSwitchEffect"/> class.
    /// </summary>
    /// <param name="obs">The OBS WebSocket service for scene control.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ObsSceneSwitchEffect(IObsWebSocketService obs, ILogger<ObsSceneSwitchEffect> logger)
    {
        _obs = obs;
        _logger = logger;
    }

    /// <summary>Switches to the configured OBS scene.</summary>
    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        string sceneName = context.GetParameter("scene_name");
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            _logger.LogWarning("OBS scene switch effect: no scene_name configured");
            return;
        }

        if (!_obs.IsConnected)
        {
            _logger.LogWarning("OBS not connected — cannot switch scene to '{Scene}'", sceneName);
            return;
        }

        await _obs.SwitchSceneAsync(sceneName, ct);
    }
}

/// <summary>Toggles the visibility of an OBS source.</summary>
public class ObsSourceToggleEffect : IEffectType
{
    private readonly IObsWebSocketService _obs;
    private readonly ILogger<ObsSourceToggleEffect> _logger;

    /// <inheritdoc />
    public string Id => "obs.source_toggle";

    /// <inheritdoc />
    public string DisplayName => "OBS: Quelle ein-/ausblenden";

    /// <inheritdoc />
    public string[] ParameterKeys => new[] { "scene_name", "source_name", "visible" };

    /// <summary>
    /// Initializes a new instance of the <see cref="ObsSourceToggleEffect"/> class.
    /// </summary>
    /// <param name="obs">The OBS WebSocket service for source control.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ObsSourceToggleEffect(IObsWebSocketService obs, ILogger<ObsSourceToggleEffect> logger)
    {
        _obs = obs;
        _logger = logger;
    }

    /// <summary>Toggles or sets the visibility of the configured OBS source.</summary>
    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        string scene = context.GetParameter("scene_name");
        string source = context.GetParameter("source_name");
        string visibleStr = context.GetParameter("visible");

        if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(source))
        {
            _logger.LogWarning("OBS source toggle: scene_name or source_name missing");
            return;
        }

        if (!_obs.IsConnected)
        {
            _logger.LogWarning("OBS not connected — cannot toggle source '{Source}'", source);
            return;
        }

        if (bool.TryParse(visibleStr, out bool visible))
        {
            await _obs.SetSourceVisibilityAsync(scene, source, visible, ct);
        }
        else
        {
            // Toggle: get current, invert
            IReadOnlyList<ObsSourceInfo> sources = await _obs.GetSourcesAsync(scene, ct);
            ObsSourceInfo? s = sources.FirstOrDefault(x =>
                string.Equals(x.SourceName, source, StringComparison.OrdinalIgnoreCase));
            if (s is not null)
            {
                await _obs.SetSourceVisibilityAsync(scene, source, !s.IsVisible, ct);
            }
        }
    }
}
