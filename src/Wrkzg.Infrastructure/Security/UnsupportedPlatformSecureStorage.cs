using System;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Security;

/// <summary>
/// Fallback secure storage for unsupported platforms (e.g. Linux).
/// All methods throw <see cref="PlatformNotSupportedException"/> with a clear message
/// instead of silently failing or causing a DI resolution crash at runtime.
/// </summary>
public class UnsupportedPlatformSecureStorage : ISecureStorage
{
    private static PlatformNotSupportedException NotSupported() =>
        new("Wrkzg secure storage is not supported on this platform. " +
            "Currently only Windows (DPAPI) and macOS (Keychain) are supported. " +
            "Please run the application on a supported operating system.");

    public Task SaveTokensAsync(TokenType type, TwitchTokens tokens, CancellationToken ct = default)
        => throw NotSupported();

    public Task<TwitchTokens?> LoadTokensAsync(TokenType type, CancellationToken ct = default)
        => throw NotSupported();

    public Task DeleteTokensAsync(TokenType type, CancellationToken ct = default)
        => throw NotSupported();

    public Task SaveClientIdAsync(string clientId, CancellationToken ct = default)
        => throw NotSupported();

    public Task<string?> LoadClientIdAsync(CancellationToken ct = default)
        => throw NotSupported();

    public Task SaveClientSecretAsync(string clientSecret, CancellationToken ct = default)
        => throw NotSupported();

    public Task<string?> LoadClientSecretAsync(CancellationToken ct = default)
        => throw NotSupported();

    public Task DeleteCredentialsAsync(CancellationToken ct = default)
        => throw NotSupported();

    public Task<bool> HasCredentialsAsync(CancellationToken ct = default)
        => throw NotSupported();
}
