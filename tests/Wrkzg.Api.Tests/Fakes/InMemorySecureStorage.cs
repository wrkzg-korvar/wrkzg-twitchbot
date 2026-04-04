using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Tests.Fakes;

/// <summary>
/// In-memory ISecureStorage for tests. Works on all platforms (no DPAPI/Keychain dependency).
/// </summary>
public class InMemorySecureStorage : ISecureStorage
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    /// <summary>Saves tokens to the in-memory store keyed by token type.</summary>
    public Task SaveTokensAsync(TokenType type, TwitchTokens tokens, CancellationToken ct = default)
    {
        _store[$"tokens:{type}"] = JsonSerializer.Serialize(tokens);
        return Task.CompletedTask;
    }

    /// <summary>Loads tokens from the in-memory store by token type.</summary>
    public Task<TwitchTokens?> LoadTokensAsync(TokenType type, CancellationToken ct = default)
    {
        if (_store.TryGetValue($"tokens:{type}", out string? json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<TwitchTokens>(json));
        }

        return Task.FromResult<TwitchTokens?>(null);
    }

    /// <summary>Removes tokens for the specified type from the in-memory store.</summary>
    public Task DeleteTokensAsync(TokenType type, CancellationToken ct = default)
    {
        _store.TryRemove($"tokens:{type}", out _);
        return Task.CompletedTask;
    }

    /// <summary>Saves the client ID to the in-memory store.</summary>
    public Task SaveClientIdAsync(string clientId, CancellationToken ct = default)
    {
        _store["clientId"] = clientId;
        return Task.CompletedTask;
    }

    /// <summary>Loads the client ID from the in-memory store.</summary>
    public Task<string?> LoadClientIdAsync(CancellationToken ct = default)
    {
        _store.TryGetValue("clientId", out string? val);
        return Task.FromResult(val);
    }

    /// <summary>Saves the client secret to the in-memory store.</summary>
    public Task SaveClientSecretAsync(string clientSecret, CancellationToken ct = default)
    {
        _store["clientSecret"] = clientSecret;
        return Task.CompletedTask;
    }

    /// <summary>Loads the client secret from the in-memory store.</summary>
    public Task<string?> LoadClientSecretAsync(CancellationToken ct = default)
    {
        _store.TryGetValue("clientSecret", out string? val);
        return Task.FromResult(val);
    }

    /// <summary>Removes both client ID and client secret from the in-memory store.</summary>
    public Task DeleteCredentialsAsync(CancellationToken ct = default)
    {
        _store.TryRemove("clientId", out _);
        _store.TryRemove("clientSecret", out _);
        return Task.CompletedTask;
    }

    /// <summary>Returns whether both client ID and client secret are present in the store.</summary>
    public Task<bool> HasCredentialsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_store.ContainsKey("clientId") && _store.ContainsKey("clientSecret"));
    }
}
