namespace Wrkzg.Core.Models;

/// <summary>
/// Bot IRC connection status, pushed to the frontend via SignalR.
/// </summary>
public class BotStatus
{
    public bool IsConnected { get; init; }
    public string? Channel { get; init; }
    public string? Reason { get; init; }
}
