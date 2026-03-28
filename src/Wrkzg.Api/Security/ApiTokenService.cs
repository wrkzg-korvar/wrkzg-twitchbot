using System;
using System.Security.Cryptography;
using System.Text;

namespace Wrkzg.Api.Security;

/// <summary>
/// Generates and holds a per-session API token that the Photino WebView
/// must include as <c>X-Wrkzg-Token</c> header on all requests.
/// Prevents other local processes and cross-origin requests from accessing the API.
/// Token is regenerated on every application start.
/// </summary>
public sealed class ApiTokenService
{
    /// <summary>The token value for this session.</summary>
    public string Token { get; }

    private readonly byte[] _tokenBytes;

    public ApiTokenService()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        Token = Convert.ToBase64String(bytes);
        _tokenBytes = Encoding.UTF8.GetBytes(Token);
    }

    /// <summary>
    /// Validates the provided token against the session token.
    /// Uses constant-time comparison to prevent timing side-channel attacks.
    /// </summary>
    public bool IsValid(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        byte[] candidateBytes = Encoding.UTF8.GetBytes(token);
        if (candidateBytes.Length != _tokenBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(_tokenBytes, candidateBytes);
    }
}
