using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Platform-specific encrypted storage for OAuth tokens and Twitch app credentials.
/// Windows: DPAPI (ProtectedData, CurrentUser scope).
/// macOS: Keychain via Security.framework.
///
/// ALL sensitive Twitch credentials (Client ID, Client Secret, OAuth tokens)
/// are stored exclusively here — never in appsettings.json or environment variables.
/// The only exception is appsettings.Development.json for contributor development.
/// </summary>
public interface ISecureStorage
{
    // ─── OAuth Tokens ─────────────────────────────────────────────────

    Task SaveTokensAsync(TokenType type, TwitchTokens tokens, CancellationToken ct = default);
    Task<TwitchTokens?> LoadTokensAsync(TokenType type, CancellationToken ct = default);
    Task DeleteTokensAsync(TokenType type, CancellationToken ct = default);

    // ─── Twitch App Credentials ───────────────────────────────────────

    /// <summary>
    /// Saves the Twitch Client ID to encrypted storage.
    /// Called during the Setup Wizard when the user enters their app credentials.
    /// </summary>
    Task SaveClientIdAsync(string clientId, CancellationToken ct = default);

    /// <summary>
    /// Loads the Twitch Client ID from encrypted storage.
    /// Returns null if not yet configured (triggers Setup Wizard).
    /// </summary>
    Task<string?> LoadClientIdAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves the Twitch Client Secret to encrypted storage.
    /// </summary>
    Task SaveClientSecretAsync(string clientSecret, CancellationToken ct = default);

    /// <summary>
    /// Loads the Twitch Client Secret from encrypted storage.
    /// Returns null if not yet configured.
    /// </summary>
    Task<string?> LoadClientSecretAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes all stored Twitch app credentials (Client ID + Secret).
    /// Used when the user wants to reconfigure the Twitch app from the Settings page.
    /// </summary>
    Task DeleteCredentialsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns true if both Client ID and Client Secret are stored.
    /// Used to determine whether the Setup Wizard should be shown.
    /// </summary>
    Task<bool> HasCredentialsAsync(CancellationToken ct = default);
}
