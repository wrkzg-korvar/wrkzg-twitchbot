namespace Wrkzg.Core.Models;

/// <summary>
/// Represents the authentication state for one token type.
/// Pushed to the frontend via SignalR whenever the state changes.
/// </summary>
public sealed class AuthState
{
    public TokenType TokenType { get; init; }
    public bool IsAuthenticated { get; init; }
    public string? TwitchUsername { get; init; }
    public string? TwitchDisplayName { get; init; }
    public string? TwitchUserId { get; init; }
    public string[]? Scopes { get; init; }
}
