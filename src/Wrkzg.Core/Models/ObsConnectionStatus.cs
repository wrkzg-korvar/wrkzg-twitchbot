namespace Wrkzg.Core.Models;

/// <summary>OBS WebSocket connection status.</summary>
public class ObsConnectionStatus
{
    /// <summary>Whether the OBS WebSocket is currently connected.</summary>
    public bool IsConnected { get; set; }

    /// <summary>OBS Studio version (null if not connected).</summary>
    public string? ObsVersion { get; set; }

    /// <summary>Current active scene name (null if not connected).</summary>
    public string? CurrentScene { get; set; }

    /// <summary>Whether connection settings are configured.</summary>
    public bool IsConfigured { get; set; }
}
