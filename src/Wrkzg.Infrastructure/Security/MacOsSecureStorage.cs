using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Security;

/// <summary>
/// macOS-specific secure storage using the system Keychain via the `security` CLI tool.
/// Each entry is stored as a generic password with Service="Wrkzg" and Account="{name}".
/// </summary>
public class MacOsSecureStorage : ISecureStorage
{
    private const string ServiceName = "Wrkzg";
    private readonly ILogger<MacOsSecureStorage> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MacOsSecureStorage"/> class.
    /// </summary>
    /// <param name="logger">The logger for Keychain operation diagnostics.</param>
    public MacOsSecureStorage(ILogger<MacOsSecureStorage> logger)
    {
        _logger = logger;
    }

    // ─── OAuth Tokens ─────────────────────────────────────────────────

    /// <summary>Saves OAuth tokens to the macOS Keychain for the specified token type.</summary>
    public async Task SaveTokensAsync(TokenType type, TwitchTokens tokens, CancellationToken ct = default)
    {
        string json = JsonSerializer.Serialize(tokens, _jsonOptions);
        await SaveToKeychainAsync($"token_{type.ToString().ToLowerInvariant()}", json, ct);
        _logger.LogDebug("Saved {TokenType} tokens to macOS Keychain", type);
    }

    /// <summary>Loads OAuth tokens from the macOS Keychain for the specified token type.</summary>
    public async Task<TwitchTokens?> LoadTokensAsync(TokenType type, CancellationToken ct = default)
    {
        string? json = await LoadFromKeychainAsync($"token_{type.ToString().ToLowerInvariant()}", ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TwitchTokens>(json.Trim(), _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize {TokenType} tokens — deleting corrupted entry", type);
            await DeleteTokensAsync(type, ct);
            return null;
        }
    }

    /// <summary>Deletes OAuth tokens from the macOS Keychain for the specified token type.</summary>
    public async Task DeleteTokensAsync(TokenType type, CancellationToken ct = default)
    {
        await DeleteFromKeychainAsync($"token_{type.ToString().ToLowerInvariant()}", ct);
        _logger.LogInformation("Deleted {TokenType} tokens from macOS Keychain", type);
    }

    // ─── Twitch App Credentials ───────────────────────────────────────

    /// <summary>Saves the Twitch application Client ID to the macOS Keychain.</summary>
    public async Task SaveClientIdAsync(string clientId, CancellationToken ct = default)
    {
        await SaveToKeychainAsync("client_id", clientId, ct);
        _logger.LogDebug("Saved Client ID to macOS Keychain");
    }

    /// <summary>Loads the Twitch application Client ID from the macOS Keychain.</summary>
    public async Task<string?> LoadClientIdAsync(CancellationToken ct = default)
    {
        string? value = await LoadFromKeychainAsync("client_id", ct);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Saves the Twitch application Client Secret to the macOS Keychain.</summary>
    public async Task SaveClientSecretAsync(string clientSecret, CancellationToken ct = default)
    {
        await SaveToKeychainAsync("client_secret", clientSecret, ct);
        _logger.LogDebug("Saved Client Secret to macOS Keychain");
    }

    /// <summary>Loads the Twitch application Client Secret from the macOS Keychain.</summary>
    public async Task<string?> LoadClientSecretAsync(CancellationToken ct = default)
    {
        string? value = await LoadFromKeychainAsync("client_secret", ct);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Deletes both Client ID and Client Secret from the macOS Keychain.</summary>
    public async Task DeleteCredentialsAsync(CancellationToken ct = default)
    {
        await DeleteFromKeychainAsync("client_id", ct);
        await DeleteFromKeychainAsync("client_secret", ct);
        _logger.LogInformation("Deleted Twitch app credentials from macOS Keychain");
    }

    /// <summary>Checks whether both Client ID and Client Secret are stored in the macOS Keychain.</summary>
    public async Task<bool> HasCredentialsAsync(CancellationToken ct = default)
    {
        string? clientId = await LoadClientIdAsync(ct);
        string? clientSecret = await LoadClientSecretAsync(ct);
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    // ─── Generic Secrets ──────────────────────────────────────────────

    /// <summary>Saves a named secret to the macOS Keychain.</summary>
    public async Task SaveSecretAsync(string key, string value, CancellationToken ct = default)
    {
        string account = $"secret_{key}";
        await SaveToKeychainAsync(account, value, ct);
        _logger.LogDebug("Saved secret '{Key}' to macOS Keychain", key);
    }

    /// <summary>Loads a named secret from the macOS Keychain.</summary>
    public async Task<string?> LoadSecretAsync(string key, CancellationToken ct = default)
    {
        string account = $"secret_{key}";
        string? value = await LoadFromKeychainAsync(account, ct);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Deletes a named secret from the macOS Keychain.</summary>
    public async Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        string account = $"secret_{key}";
        await DeleteFromKeychainAsync(account, ct);
        _logger.LogDebug("Deleted secret '{Key}' from macOS Keychain", key);
    }

    // ─── Low-Level Keychain Helpers ───────────────────────────────────

    private async Task SaveToKeychainAsync(string account, string value, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            // Delete existing entry first (add fails if entry exists)
            await RunSecurityAsync(new[] { "delete-generic-password", "-s", ServiceName, "-a", account }, throwOnError: false);
            await RunSecurityAsync(new[] { "add-generic-password", "-s", ServiceName, "-a", account, "-w", value, "-U" });
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string?> LoadFromKeychainAsync(string account, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await RunSecurityAsync(
                new[] { "find-generic-password", "-s", ServiceName, "-a", account, "-w" },
                throwOnError: false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task DeleteFromKeychainAsync(string account, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await RunSecurityAsync(
                new[] { "delete-generic-password", "-s", ServiceName, "-a", account },
                throwOnError: false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Runs the macOS <c>security</c> CLI using ArgumentList (not shell-interpolated Arguments)
    /// to prevent shell injection attacks.
    /// </summary>
    private async Task<string?> RunSecurityAsync(string[] args, bool throwOnError = true)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "/usr/bin/security",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using Process? process = Process.Start(psi);
        if (process is null)
        {
            if (throwOnError)
            {
                throw new InvalidOperationException("Failed to start macOS security command.");
            }

            return null;
        }

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            if (throwOnError)
            {
                throw new InvalidOperationException(
                    $"macOS security command failed (exit {process.ExitCode}): {stderr}");
            }

            _logger.LogDebug("macOS security command returned exit code {ExitCode}: {Stderr}",
                process.ExitCode, stderr.Trim());
            return null;
        }

        return stdout;
    }
}
