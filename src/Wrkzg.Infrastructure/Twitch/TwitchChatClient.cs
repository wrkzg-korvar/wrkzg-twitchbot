using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using ChatMessage = Wrkzg.Core.Models.ChatMessage;
using ConnectionCredentials = TwitchLib.Client.Models.ConnectionCredentials;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// Twitch IRC chat client implemented via TwitchLib.Client.
/// Singleton — maintains connection state across the application lifetime.
/// </summary>
public class TwitchChatClient : ITwitchChatClient
{
    private readonly ISecureStorage _storage;
    private readonly ITwitchOAuthService _oauth;
    private readonly ILogger<TwitchChatClient> _logger;

    private TwitchClient? _client;
    private string? _joinedChannel;
    private string? _botUsername;

    public event Func<ChatMessage, Task>? OnMessageReceived;
    public event Func<string, Task>? OnUserJoined;
    public event Func<Task>? OnConnected;
    public event Func<Task>? OnDisconnected;

    public bool IsConnected => _client?.IsConnected ?? false;
    public string? JoinedChannel => _joinedChannel;

    public TwitchChatClient(
        ISecureStorage storage,
        ITwitchOAuthService oauth,
        ILogger<TwitchChatClient> logger)
    {
        _storage = storage;
        _oauth = oauth;
        _logger = logger;
    }

    public async Task ConnectAsync(string channel, CancellationToken ct = default)
    {
        if (_client?.IsConnected == true)
        {
            _logger.LogWarning("Already connected to IRC — disconnecting first");
            await DisconnectAsync(ct);
        }

        // Load and validate Bot token
        TwitchTokens? tokens = await _storage.LoadTokensAsync(TokenType.Bot, ct);
        if (tokens is null)
        {
            throw new InvalidOperationException(
                "No Bot token available. Please connect your bot account first.");
        }

        tokens = await EnsureValidTokenAsync(tokens, ct);
        string botUsername = await GetBotUsernameAsync(tokens.AccessToken, ct);
        _botUsername = botUsername;

        _logger.LogInformation("Connecting to Twitch IRC as {BotUsername} in channel #{Channel}",
            botUsername, channel);

        // Configure TwitchLib
        ClientOptions clientOptions = new()
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30),
            ReconnectionPolicy = new ReconnectionPolicy(3000, maxAttempts: 10)
        };

        WebSocketClient wsClient = new(clientOptions);
        _client = new TwitchClient(wsClient);

        ConnectionCredentials credentials = new(botUsername, tokens.AccessToken);

        _client.Initialize(credentials, channel);

        // Wire up events
        _client.OnMessageReceived += HandleMessageReceived;
        _client.OnUserJoined += HandleUserJoined;
        _client.OnConnected += HandleConnected;
        _client.OnDisconnected += HandleDisconnected;
        _client.OnLog += HandleLog;
        _client.OnConnectionError += HandleConnectionError;
        _client.OnReconnected += HandleReconnected;

        _client.Connect();
        _joinedChannel = channel;

        _logger.LogInformation("IRC connection initiated for channel #{Channel}", channel);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_client is null)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Disconnecting from Twitch IRC");

        try
        {
            if (_client.IsConnected)
            {
                _client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during IRC disconnect — continuing");
        }

        _joinedChannel = null;
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(string message, CancellationToken ct = default)
    {
        if (_client is null || !_client.IsConnected || _joinedChannel is null)
        {
            _logger.LogWarning("Cannot send message — not connected to IRC");
            return Task.CompletedTask;
        }

        _client.SendMessage(_joinedChannel, message);
        return Task.CompletedTask;
    }

    public Task SendReplyAsync(string replyToMessageId, string message, CancellationToken ct = default)
    {
        if (_client is null || !_client.IsConnected || _joinedChannel is null)
        {
            _logger.LogWarning("Cannot send reply — not connected to IRC");
            return Task.CompletedTask;
        }

        _client.SendReply(_joinedChannel, replyToMessageId, message);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();

        if (_client is not null)
        {
            _client.OnMessageReceived -= HandleMessageReceived;
            _client.OnUserJoined -= HandleUserJoined;
            _client.OnConnected -= HandleConnected;
            _client.OnDisconnected -= HandleDisconnected;
            _client.OnLog -= HandleLog;
            _client.OnConnectionError -= HandleConnectionError;
            _client.OnReconnected -= HandleReconnected;
        }

        _client = null;
        GC.SuppressFinalize(this);
    }

    // ─── Event Handlers ───────────────────────────────────────────────

    private async void HandleMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        if (OnMessageReceived is null) { return; }

        // Skip messages from the bot itself to prevent loops
        if (_botUsername is not null
            && string.Equals(e.ChatMessage.Username, _botUsername, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            ChatMessage mapped = MapMessage(e.ChatMessage);
            await OnMessageReceived(mapped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message from {User}", e.ChatMessage.Username);
        }
    }

    private async void HandleUserJoined(object? sender, OnUserJoinedArgs e)
    {
        if (OnUserJoined is null) { return; }

        try { await OnUserJoined(e.Username); }
        catch (Exception ex) { _logger.LogError(ex, "Error processing user join for {User}", e.Username); }
    }

    private async void HandleConnected(object? sender, OnConnectedArgs e)
    {
        _logger.LogInformation("Connected to Twitch IRC — bot: {BotUsername}, channel: #{Channel}",
            e.BotUsername, _joinedChannel);

        if (OnConnected is not null)
        {
            try { await OnConnected(); }
            catch (Exception ex) { _logger.LogError(ex, "Error in OnConnected handler"); }
        }
    }

    private async void HandleDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        _logger.LogWarning("Disconnected from Twitch IRC");

        if (OnDisconnected is not null)
        {
            try { await OnDisconnected(); }
            catch (Exception ex) { _logger.LogError(ex, "Error in OnDisconnected handler"); }
        }
    }

    private void HandleLog(object? sender, OnLogArgs e)
    {
        _logger.LogDebug("TwitchLib: {Data}", e.Data);
    }

    private void HandleConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        _logger.LogError("TwitchLib connection error: {Message}", e.Error.Message);
    }

    private void HandleReconnected(object? sender, OnReconnectedEventArgs e)
    {
        _logger.LogInformation("Reconnected to Twitch IRC");
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private async Task<TwitchTokens> EnsureValidTokenAsync(TwitchTokens tokens, CancellationToken ct)
    {
        if (!tokens.IsLikelyExpired)
        {
            TwitchTokenValidation? validation = await _oauth.ValidateTokenAsync(tokens.AccessToken, ct);
            if (validation is not null)
            {
                return tokens;
            }
        }

        _logger.LogInformation("Bot token expired or invalid — refreshing");

        try
        {
            TwitchTokens refreshed = await _oauth.RefreshTokenAsync(tokens.RefreshToken, ct);
            await _storage.SaveTokensAsync(TokenType.Bot, refreshed, ct);
            return refreshed;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Bot token refresh failed. Please reconnect your bot account.", ex);
        }
    }

    private async Task<string> GetBotUsernameAsync(string accessToken, CancellationToken ct)
    {
        TwitchTokenValidation? validation = await _oauth.ValidateTokenAsync(accessToken, ct);
        if (validation is null)
        {
            throw new InvalidOperationException("Failed to validate bot token — cannot determine username.");
        }

        return validation.Login;
    }

    private static ChatMessage MapMessage(TwitchLib.Client.Models.ChatMessage msg)
    {
        return new ChatMessage(
            UserId: msg.UserId,
            Username: msg.Username,
            DisplayName: msg.DisplayName,
            Content: msg.Message,
            IsModerator: msg.IsModerator,
            IsSubscriber: msg.IsSubscriber,
            IsBroadcaster: msg.IsBroadcaster,
            Timestamp: DateTimeOffset.UtcNow)
        {
            Channel = msg.Channel
        };
    }
}
