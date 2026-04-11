using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Manages the OBS WebSocket 5.x connection and provides scene/source control.
/// Singleton service — one connection per app instance.
/// </summary>
public interface IObsWebSocketService
{
    /// <summary>Gets the current connection status.</summary>
    ObsConnectionStatus GetStatus();

    /// <summary>Connects to OBS WebSocket with stored settings.</summary>
    Task<bool> ConnectAsync(CancellationToken ct = default);

    /// <summary>Disconnects from OBS WebSocket.</summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>Gets the list of available scenes.</summary>
    Task<IReadOnlyList<string>> GetScenesAsync(CancellationToken ct = default);

    /// <summary>Switches to a scene by name.</summary>
    Task<bool> SwitchSceneAsync(string sceneName, CancellationToken ct = default);

    /// <summary>Gets source items in a scene.</summary>
    Task<IReadOnlyList<ObsSourceInfo>> GetSourcesAsync(string? sceneName = null, CancellationToken ct = default);

    /// <summary>Toggles a source's visibility.</summary>
    Task<bool> SetSourceVisibilityAsync(string sceneName, string sourceName, bool visible, CancellationToken ct = default);

    /// <summary>Whether the service is currently connected to OBS.</summary>
    bool IsConnected { get; }
}

/// <summary>OBS source/item information.</summary>
public class ObsSourceInfo
{
    /// <summary>Scene item ID.</summary>
    public int SceneItemId { get; init; }

    /// <summary>Source name.</summary>
    public string SourceName { get; init; } = string.Empty;

    /// <summary>Whether the source is currently visible.</summary>
    public bool IsVisible { get; init; }
}
