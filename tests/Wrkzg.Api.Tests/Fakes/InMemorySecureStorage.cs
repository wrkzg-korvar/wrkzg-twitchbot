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

    public Task SaveTokensAsync(TokenType type, TwitchTokens tokens, CancellationToken ct = default)
    {
        _store[$"tokens:{type}"] = JsonSerializer.Serialize(tokens);
        return Task.CompletedTask;
    }

    public Task<TwitchTokens?> LoadTokensAsync(TokenType type, CancellationToken ct = default)
    {
        if (_store.TryGetValue($"tokens:{type}", out string? json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<TwitchTokens>(json));
        }

        return Task.FromResult<TwitchTokens?>(null);
    }

    public Task DeleteTokensAsync(TokenType type, CancellationToken ct = default)
    {
        _store.TryRemove($"tokens:{type}", out _);
        return Task.CompletedTask;
    }

    public Task SaveClientIdAsync(string clientId, CancellationToken ct = default)
    {
        _store["clientId"] = clientId;
        return Task.CompletedTask;
    }

    public Task<string?> LoadClientIdAsync(CancellationToken ct = default)
    {
        _store.TryGetValue("clientId", out string? val);
        return Task.FromResult(val);
    }

    public Task SaveClientSecretAsync(string clientSecret, CancellationToken ct = default)
    {
        _store["clientSecret"] = clientSecret;
        return Task.CompletedTask;
    }

    public Task<string?> LoadClientSecretAsync(CancellationToken ct = default)
    {
        _store.TryGetValue("clientSecret", out string? val);
        return Task.FromResult(val);
    }

    public Task DeleteCredentialsAsync(CancellationToken ct = default)
    {
        _store.TryRemove("clientId", out _);
        _store.TryRemove("clientSecret", out _);
        return Task.CompletedTask;
    }

    public Task<bool> HasCredentialsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_store.ContainsKey("clientId") && _store.ContainsKey("clientSecret"));
    }
}
