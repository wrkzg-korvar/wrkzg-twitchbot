using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Security;

/// <summary>
/// Windows-specific secure storage using DPAPI (Data Protection API).
/// Encrypted with the current user's Windows account — only this user can decrypt.
/// Files are stored in %APPDATA%\Wrkzg\tokens\
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSecureStorage : ISecureStorage
{
    private readonly string _storagePath;
    private readonly ILogger<WindowsSecureStorage> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsSecureStorage"/> class
    /// and ensures the encrypted token storage directory exists.
    /// </summary>
    /// <param name="logger">The logger for DPAPI operation diagnostics.</param>
    public WindowsSecureStorage(ILogger<WindowsSecureStorage> logger)
    {
        _logger = logger;
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wrkzg", "tokens");

        Directory.CreateDirectory(_storagePath);
    }

    // ─── OAuth Tokens ─────────────────────────────────────────────────

    /// <summary>Saves OAuth tokens encrypted with DPAPI for the specified token type.</summary>
    public async Task SaveTokensAsync(TokenType type, TwitchTokens tokens, CancellationToken ct = default)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(tokens, _jsonOptions);
        await SaveEncryptedAsync(GetTokenFilePath(type), json, ct);
        _logger.LogDebug("Saved {TokenType} tokens", type);
    }

    /// <summary>Loads and decrypts OAuth tokens for the specified token type.</summary>
    public async Task<TwitchTokens?> LoadTokensAsync(TokenType type, CancellationToken ct = default)
    {
        byte[]? json = await LoadEncryptedAsync(GetTokenFilePath(type), ct);
        if (json is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TwitchTokens>(json, _jsonOptions);
    }

    /// <summary>Deletes the encrypted token file for the specified token type.</summary>
    public Task DeleteTokensAsync(TokenType type, CancellationToken ct = default)
    {
        DeleteFile(GetTokenFilePath(type));
        _logger.LogInformation("Deleted {TokenType} tokens", type);
        return Task.CompletedTask;
    }

    // ─── Twitch App Credentials ───────────────────────────────────────

    /// <summary>Saves the Twitch application Client ID encrypted with DPAPI.</summary>
    public async Task SaveClientIdAsync(string clientId, CancellationToken ct = default)
    {
        byte[] raw = Encoding.UTF8.GetBytes(clientId);
        await SaveEncryptedAsync(GetCredentialFilePath("client_id"), raw, ct);
        _logger.LogDebug("Saved Client ID to secure storage");
    }

    /// <summary>Loads and decrypts the Twitch application Client ID.</summary>
    public async Task<string?> LoadClientIdAsync(CancellationToken ct = default)
    {
        byte[]? raw = await LoadEncryptedAsync(GetCredentialFilePath("client_id"), ct);
        return raw is null ? null : Encoding.UTF8.GetString(raw);
    }

    /// <summary>Saves the Twitch application Client Secret encrypted with DPAPI.</summary>
    public async Task SaveClientSecretAsync(string clientSecret, CancellationToken ct = default)
    {
        byte[] raw = Encoding.UTF8.GetBytes(clientSecret);
        await SaveEncryptedAsync(GetCredentialFilePath("client_secret"), raw, ct);
        _logger.LogDebug("Saved Client Secret to secure storage");
    }

    /// <summary>Loads and decrypts the Twitch application Client Secret.</summary>
    public async Task<string?> LoadClientSecretAsync(CancellationToken ct = default)
    {
        byte[]? raw = await LoadEncryptedAsync(GetCredentialFilePath("client_secret"), ct);
        return raw is null ? null : Encoding.UTF8.GetString(raw);
    }

    /// <summary>Deletes both Client ID and Client Secret encrypted files.</summary>
    public Task DeleteCredentialsAsync(CancellationToken ct = default)
    {
        DeleteFile(GetCredentialFilePath("client_id"));
        DeleteFile(GetCredentialFilePath("client_secret"));
        _logger.LogInformation("Deleted Twitch app credentials from secure storage");
        return Task.CompletedTask;
    }

    /// <summary>Checks whether both Client ID and Client Secret encrypted files exist and are readable.</summary>
    public async Task<bool> HasCredentialsAsync(CancellationToken ct = default)
    {
        string? clientId = await LoadClientIdAsync(ct);
        string? clientSecret = await LoadClientSecretAsync(ct);
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    // ─── Generic Secrets ──────────────────────────────────────────────

    /// <summary>Saves a named secret encrypted with DPAPI.</summary>
    public async Task SaveSecretAsync(string key, string value, CancellationToken ct = default)
    {
        byte[] raw = Encoding.UTF8.GetBytes(value);
        await SaveEncryptedAsync(GetSecretFilePath(key), raw, ct);
        _logger.LogDebug("Saved secret '{Key}' to secure storage", key);
    }

    /// <summary>Loads and decrypts a named secret.</summary>
    public async Task<string?> LoadSecretAsync(string key, CancellationToken ct = default)
    {
        byte[]? raw = await LoadEncryptedAsync(GetSecretFilePath(key), ct);
        return raw is null ? null : Encoding.UTF8.GetString(raw);
    }

    /// <summary>Deletes a named secret file.</summary>
    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        DeleteFile(GetSecretFilePath(key));
        _logger.LogDebug("Deleted secret '{Key}' from secure storage", key);
        return Task.CompletedTask;
    }

    // ─── Low-Level DPAPI Helpers ──────────────────────────────────────

    private async Task SaveEncryptedAsync(string filePath, byte[] data, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(filePath, encrypted, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<byte[]?> LoadEncryptedAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        await _lock.WaitAsync(ct);
        try
        {
            byte[] encrypted = await File.ReadAllBytesAsync(filePath, ct);
            return ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt {Path} — deleting corrupted file", filePath);
            File.Delete(filePath);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetTokenFilePath(TokenType type)
    {
        return Path.Combine(_storagePath, $"{type.ToString().ToLowerInvariant()}.enc");
    }

    private string GetCredentialFilePath(string name)
    {
        return Path.Combine(_storagePath, $"{name}.enc");
    }

    private string GetSecretFilePath(string key)
    {
        // Sanitize key for filename
        string safe = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_storagePath, $"secret_{safe}.enc");
    }
}
