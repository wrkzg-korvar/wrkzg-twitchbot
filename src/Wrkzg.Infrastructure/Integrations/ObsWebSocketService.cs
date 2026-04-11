using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Integrations;

/// <summary>
/// OBS WebSocket 5.x client using raw <see cref="ClientWebSocket"/>.
/// Singleton service — one connection per app instance.
/// Supports: GetSceneList, SetCurrentProgramScene, GetSceneItemList, SetSceneItemEnabled, GetVersion.
/// </summary>
public class ObsWebSocketService : IObsWebSocketService, IDisposable
{
    private readonly ISecureStorage _secureStorage;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ObsWebSocketService> _logger;

    private ClientWebSocket? _ws;
    private volatile bool _isConnected;
    private string? _obsVersion;
    private string? _currentScene;
    private CancellationTokenSource? _receiveCts;
    private int _nextRequestId;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // OBS WebSocket 5.x opcodes
    private const int OpHello = 0;
    private const int OpIdentify = 1;
    private const int OpIdentified = 2;
    private const int OpRequest = 6;
    private const int OpRequestResponse = 7;

    // Settings keys
    private const string SettingHost = "Integration.Obs.Host";
    private const string SettingPort = "Integration.Obs.Port";
    private const string SecretPassword = "obs-websocket-password";

    /// <summary>
    /// Initializes a new instance of the <see cref="ObsWebSocketService"/> class.
    /// </summary>
    /// <param name="secureStorage">Encrypted storage for the OBS WebSocket password.</param>
    /// <param name="scopeFactory">Factory for creating scoped service providers to access settings.</param>
    /// <param name="logger">Logger for OBS WebSocket diagnostics.</param>
    public ObsWebSocketService(
        ISecureStorage secureStorage,
        IServiceScopeFactory scopeFactory,
        ILogger<ObsWebSocketService> logger)
    {
        _secureStorage = secureStorage;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public ObsConnectionStatus GetStatus()
    {
        bool isConfigured;
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            string? host = settings.GetAsync(SettingHost).GetAwaiter().GetResult();
            isConfigured = !string.IsNullOrWhiteSpace(host);
        }

        return new ObsConnectionStatus
        {
            IsConnected = _isConnected,
            ObsVersion = _obsVersion,
            CurrentScene = _currentScene,
            IsConfigured = isConfigured,
        };
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        // Disconnect if already connected
        if (_isConnected)
        {
            await DisconnectAsync(ct);
        }

        string host;
        int port;
        string? password;

        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            host = await settings.GetAsync(SettingHost, ct) ?? "localhost";
            string? portStr = await settings.GetAsync(SettingPort, ct) ?? "4455";
            if (!int.TryParse(portStr, out port))
            {
                port = 4455;
            }
        }

        password = await _secureStorage.LoadSecretAsync(SecretPassword, ct);

        try
        {
            _ws = new ClientWebSocket();
            Uri uri = new($"ws://{host}:{port}");
            await _ws.ConnectAsync(uri, ct);

            // OBS WS 5.x handshake: receive Hello, send Identify, receive Identified
            JsonElement hello = await ReceiveSingleMessageAsync(ct);
            int helloOp = hello.GetProperty("op").GetInt32();
            if (helloOp != OpHello)
            {
                _logger.LogWarning("Expected OBS Hello (op=0), got op={Op}", helloOp);
                await CleanupWebSocketAsync();
                return false;
            }

            JsonElement helloData = hello.GetProperty("d");

            // Build Identify message
            JsonElement identifyData = BuildIdentifyPayload(helloData, password);
            await SendMessageAsync(OpIdentify, identifyData, ct);

            // Receive Identified
            JsonElement identified = await ReceiveSingleMessageAsync(ct);
            int identifiedOp = identified.GetProperty("op").GetInt32();
            if (identifiedOp != OpIdentified)
            {
                _logger.LogWarning("OBS identification failed (op={Op})", identifiedOp);
                await CleanupWebSocketAsync();
                return false;
            }

            _isConnected = true;

            // Start receive loop for ongoing messages
            _receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), CancellationToken.None);

            _logger.LogInformation("Connected to OBS WebSocket at {Host}:{Port}", host, port);

            // Get initial state
            await RefreshStateAsync(ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to OBS WebSocket at {Host}:{Port}", host, port);
            _isConnected = false;
            await CleanupWebSocketAsync();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        _isConnected = false;
        _obsVersion = null;
        _currentScene = null;

        if (_receiveCts is not null)
        {
            await _receiveCts.CancelAsync();
            _receiveCts.Dispose();
            _receiveCts = null;
        }

        await CleanupWebSocketAsync();
        _logger.LogInformation("Disconnected from OBS WebSocket");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetScenesAsync(CancellationToken ct = default)
    {
        if (!_isConnected)
        {
            return Array.Empty<string>();
        }

        try
        {
            JsonElement response = await SendRequestAsync("GetSceneList", null, ct);
            JsonElement scenes = response.GetProperty("scenes");
            List<string> result = new();
            foreach (JsonElement scene in scenes.EnumerateArray())
            {
                string? name = scene.GetProperty("sceneName").GetString();
                if (name is not null)
                {
                    result.Add(name);
                }
            }

            // OBS returns scenes in reverse order (last = first in UI)
            result.Reverse();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OBS scenes");
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SwitchSceneAsync(string sceneName, CancellationToken ct = default)
    {
        if (!_isConnected)
        {
            return false;
        }

        try
        {
            JsonElement requestData = JsonSerializer.SerializeToElement(new { sceneName });
            await SendRequestAsync("SetCurrentProgramScene", requestData, ct);
            _currentScene = sceneName;
            _logger.LogInformation("Switched OBS scene to '{Scene}'", sceneName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to switch OBS scene to '{Scene}'", sceneName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ObsSourceInfo>> GetSourcesAsync(string? sceneName = null, CancellationToken ct = default)
    {
        if (!_isConnected)
        {
            return Array.Empty<ObsSourceInfo>();
        }

        try
        {
            string scene = sceneName ?? _currentScene ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scene))
            {
                return Array.Empty<ObsSourceInfo>();
            }

            JsonElement requestData = JsonSerializer.SerializeToElement(new { sceneName = scene });
            JsonElement response = await SendRequestAsync("GetSceneItemList", requestData, ct);
            JsonElement items = response.GetProperty("sceneItems");
            List<ObsSourceInfo> result = new();
            foreach (JsonElement item in items.EnumerateArray())
            {
                result.Add(new ObsSourceInfo
                {
                    SceneItemId = item.GetProperty("sceneItemId").GetInt32(),
                    SourceName = item.GetProperty("sourceName").GetString() ?? string.Empty,
                    IsVisible = item.GetProperty("sceneItemEnabled").GetBoolean(),
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OBS sources for scene '{Scene}'", sceneName);
            return Array.Empty<ObsSourceInfo>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetSourceVisibilityAsync(string sceneName, string sourceName, bool visible, CancellationToken ct = default)
    {
        if (!_isConnected)
        {
            return false;
        }

        try
        {
            // First, find the scene item ID for the source name
            IReadOnlyList<ObsSourceInfo> sources = await GetSourcesAsync(sceneName, ct);
            ObsSourceInfo? source = sources.FirstOrDefault(s =>
                string.Equals(s.SourceName, sourceName, StringComparison.OrdinalIgnoreCase));

            if (source is null)
            {
                _logger.LogWarning("OBS source '{Source}' not found in scene '{Scene}'", sourceName, sceneName);
                return false;
            }

            JsonElement requestData = JsonSerializer.SerializeToElement(new
            {
                sceneName,
                sceneItemId = source.SceneItemId,
                sceneItemEnabled = visible
            });
            await SendRequestAsync("SetSceneItemEnabled", requestData, ct);

            _logger.LogInformation("Set OBS source '{Source}' visibility to {Visible} in scene '{Scene}'",
                sourceName, visible, sceneName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set OBS source visibility for '{Source}'", sourceName);
            return false;
        }
    }

    /// <summary>Disposes the WebSocket connection and cancellation token source.</summary>
    public void Dispose()
    {
        _isConnected = false;
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _ws?.Dispose();
        _sendLock.Dispose();
        GC.SuppressFinalize(this);
    }

    // ─── OBS WS 5.x Protocol Helpers ─────────────────────────────

    private async Task RefreshStateAsync(CancellationToken ct)
    {
        try
        {
            // Get version
            JsonElement versionResponse = await SendRequestAsync("GetVersion", null, ct);
            _obsVersion = versionResponse.GetProperty("obsVersion").GetString();

            // Get current scene
            JsonElement sceneResponse = await SendRequestAsync("GetCurrentProgramScene", null, ct);
            _currentScene = sceneResponse.GetProperty("currentProgramSceneName").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh OBS state after connect");
        }
    }

    private JsonElement BuildIdentifyPayload(JsonElement helloData, string? password)
    {
        int rpcVersion = 1;
        if (helloData.TryGetProperty("obsWebSocketVersion", out JsonElement _))
        {
            rpcVersion = helloData.TryGetProperty("rpcVersion", out JsonElement rpcEl)
                ? rpcEl.GetInt32()
                : 1;
        }

        if (helloData.TryGetProperty("authentication", out JsonElement authData) && !string.IsNullOrWhiteSpace(password))
        {
            string challenge = authData.GetProperty("challenge").GetString()!;
            string salt = authData.GetProperty("salt").GetString()!;
            string authResponse = GenerateAuthResponse(password, salt, challenge);

            return JsonSerializer.SerializeToElement(new
            {
                rpcVersion,
                authentication = authResponse
            });
        }

        return JsonSerializer.SerializeToElement(new { rpcVersion });
    }

    private static string GenerateAuthResponse(string password, string salt, string challenge)
    {
        // OBS WS 5.x auth: base64(sha256(base64(sha256(password + salt)) + challenge))
        byte[] passwordSaltHash = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        string base64Secret = Convert.ToBase64String(passwordSaltHash);
        byte[] challengeHash = SHA256.HashData(Encoding.UTF8.GetBytes(base64Secret + challenge));
        return Convert.ToBase64String(challengeHash);
    }

    private async Task<JsonElement> SendRequestAsync(string requestType, JsonElement? requestData, CancellationToken ct)
    {
        string requestId = Interlocked.Increment(ref _nextRequestId).ToString();
        TaskCompletionSource<JsonElement> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[requestId] = tcs;

        try
        {
            object request;
            if (requestData.HasValue && requestData.Value.ValueKind != JsonValueKind.Null)
            {
                request = new { requestType, requestId, requestData = requestData.Value };
            }
            else
            {
                request = new { requestType, requestId };
            }

            JsonElement data = JsonSerializer.SerializeToElement(request);
            await SendMessageAsync(OpRequest, data, ct);

            // Wait for response with timeout
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completedTask != tcs.Task)
            {
                throw new TimeoutException($"OBS request '{requestType}' timed out after 10 seconds");
            }

            return await tcs.Task;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private async Task SendMessageAsync(int opCode, JsonElement data, CancellationToken ct)
    {
        if (_ws is null || _ws.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        object message = new { op = opCode, d = data };
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message);

        await _sendLock.WaitAsync(ct);
        try
        {
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<JsonElement> ReceiveSingleMessageAsync(CancellationToken ct)
    {
        if (_ws is null)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
        try
        {
            int totalBytes = 0;
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes), ct);
                totalBytes += result.Count;

                if (!result.EndOfMessage && totalBytes >= buffer.Length)
                {
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, newBuffer, 0, totalBytes);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("WebSocket closed during receive");
            }

            return JsonSerializer.Deserialize<JsonElement>(buffer.AsSpan(0, totalBytes));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
        try
        {
            while (!ct.IsCancellationRequested && _ws is not null && _ws.State == WebSocketState.Open)
            {
                int totalBytes = 0;
                WebSocketReceiveResult result;
                try
                {
                    do
                    {
                        result = await _ws.ReceiveAsync(
                            new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes), ct);
                        totalBytes += result.Count;

                        if (!result.EndOfMessage && totalBytes >= buffer.Length)
                        {
                            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                            Buffer.BlockCopy(buffer, 0, newBuffer, 0, totalBytes);
                            ArrayPool<byte>.Shared.Return(buffer);
                            buffer = newBuffer;
                        }
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("OBS WebSocket closed by server");
                        _isConnected = false;
                        return;
                    }

                    JsonElement message = JsonSerializer.Deserialize<JsonElement>(buffer.AsSpan(0, totalBytes));
                    HandleMessage(message);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex)
                {
                    _logger.LogWarning(ex, "OBS WebSocket receive error");
                    _isConnected = false;
                    return;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _isConnected = false;
        }
    }

    private void HandleMessage(JsonElement message)
    {
        if (!message.TryGetProperty("op", out JsonElement opElement))
        {
            return;
        }

        int op = opElement.GetInt32();

        if (op == OpRequestResponse && message.TryGetProperty("d", out JsonElement data))
        {
            if (data.TryGetProperty("requestId", out JsonElement reqIdEl))
            {
                string? requestId = reqIdEl.GetString();
                if (requestId is not null && _pendingRequests.TryGetValue(requestId, out TaskCompletionSource<JsonElement>? tcs))
                {
                    if (data.TryGetProperty("requestStatus", out JsonElement status) &&
                        status.TryGetProperty("result", out JsonElement resultEl) &&
                        !resultEl.GetBoolean())
                    {
                        string comment = status.TryGetProperty("comment", out JsonElement commentEl)
                            ? commentEl.GetString() ?? "Request failed"
                            : "Request failed";
                        tcs.TrySetException(new InvalidOperationException($"OBS request failed: {comment}"));
                    }
                    else
                    {
                        JsonElement responseData = data.TryGetProperty("responseData", out JsonElement rd)
                            ? rd
                            : default;
                        tcs.TrySetResult(responseData);
                    }
                }
            }
        }
    }

    private async Task CleanupWebSocketAsync()
    {
        if (_ws is not null)
        {
            try
            {
                if (_ws.State == WebSocketState.Open)
                {
                    using CancellationTokenSource closeCts = new(TimeSpan.FromSeconds(2));
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", closeCts.Token);
                }
            }
            catch
            {
                // Ignore close errors
            }
            finally
            {
                _ws.Dispose();
                _ws = null;
            }
        }

        // Complete any pending requests with cancellation
        foreach (KeyValuePair<string, TaskCompletionSource<JsonElement>> kvp in _pendingRequests)
        {
            kvp.Value.TrySetCanceled();
        }

        _pendingRequests.Clear();
    }
}
