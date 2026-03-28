using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// Manages the EventSub WebSocket connection lifecycle.
/// Subscribes to channel.follow, channel.subscribe, channel.subscription.gift,
/// channel.subscription.message, and channel.raid events.
/// Dispatches events to chat (configurable templates) and dashboard (SignalR).
/// </summary>
public class EventSubConnectionService : IHostedService
{
    private readonly EventSubWebsocketClient _eventSub;
    private readonly ITwitchChatClient _chatClient;
    private readonly ISecureStorage _storage;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ILogger<EventSubConnectionService> _logger;
    private readonly HttpClient _http;

    private string? _broadcasterId;
    private string? _broadcasterAccessToken;
    private CancellationTokenSource? _cts;

    public EventSubConnectionService(
        EventSubWebsocketClient eventSub,
        ITwitchChatClient chatClient,
        ISecureStorage storage,
        IServiceScopeFactory scopeFactory,
        IChatEventBroadcaster broadcaster,
        IHttpClientFactory httpClientFactory,
        ILogger<EventSubConnectionService> logger)
    {
        _eventSub = eventSub;
        _chatClient = chatClient;
        _storage = storage;
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _http = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("EventSubConnectionService starting");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _eventSub.WebsocketConnected += OnWebsocketConnected;
        _eventSub.WebsocketDisconnected += OnWebsocketDisconnected;
        _eventSub.WebsocketReconnected += OnWebsocketReconnected;
        _eventSub.ErrorOccurred += OnErrorOccurred;

        _eventSub.ChannelFollow += OnChannelFollow;
        _eventSub.ChannelSubscribe += OnChannelSubscribe;
        _eventSub.ChannelSubscriptionGift += OnChannelSubscriptionGift;
        _eventSub.ChannelSubscriptionMessage += OnChannelSubscriptionMessage;
        _eventSub.ChannelRaid += OnChannelRaid;

        await TryConnectAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("EventSubConnectionService stopping");
        _cts?.Cancel();

        _eventSub.ChannelFollow -= OnChannelFollow;
        _eventSub.ChannelSubscribe -= OnChannelSubscribe;
        _eventSub.ChannelSubscriptionGift -= OnChannelSubscriptionGift;
        _eventSub.ChannelSubscriptionMessage -= OnChannelSubscriptionMessage;
        _eventSub.ChannelRaid -= OnChannelRaid;

        _eventSub.WebsocketConnected -= OnWebsocketConnected;
        _eventSub.WebsocketDisconnected -= OnWebsocketDisconnected;
        _eventSub.WebsocketReconnected -= OnWebsocketReconnected;
        _eventSub.ErrorOccurred -= OnErrorOccurred;

        await _eventSub.DisconnectAsync();
    }

    // ─── Connection ───────────────────────────────────────────────────

    private async Task TryConnectAsync(CancellationToken ct)
    {
        try
        {
            TwitchTokens? broadcasterToken = await _storage.LoadTokensAsync(TokenType.Broadcaster, ct);
            if (broadcasterToken is null)
            {
                _logger.LogInformation(
                    "No Broadcaster token stored — EventSub disabled. " +
                    "Connect your broadcaster account in Settings.");
                return;
            }

            _broadcasterAccessToken = broadcasterToken.AccessToken;

            using IServiceScope scope = _scopeFactory.CreateScope();
            ITwitchOAuthService oauth = scope.ServiceProvider.GetRequiredService<ITwitchOAuthService>();
            TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(_broadcasterAccessToken, ct);

            if (validation is null)
            {
                _logger.LogInformation("Broadcaster token expired — refreshing");
                TwitchTokens refreshed = await oauth.RefreshTokenAsync(broadcasterToken.RefreshToken, ct);
                await _storage.SaveTokensAsync(TokenType.Broadcaster, refreshed, ct);
                _broadcasterAccessToken = refreshed.AccessToken;
                validation = await oauth.ValidateTokenAsync(_broadcasterAccessToken, ct);
            }

            if (validation is null)
            {
                _logger.LogError("Broadcaster token validation failed after refresh — EventSub disabled");
                return;
            }

            _broadcasterId = validation.UserId;
            _logger.LogInformation("EventSub connecting for broadcaster {BroadcasterId}", _broadcasterId);

            await _eventSub.ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventSub connection failed");
        }
    }

    // ─── WebSocket Lifecycle Events ───────────────────────────────────

    private async Task OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        _logger.LogInformation("EventSub WebSocket connected — session {SessionId}",
            _eventSub.SessionId);

        if (!e.IsRequestedReconnect)
        {
            await SubscribeToEventsAsync();
        }
    }

    private async Task OnWebsocketDisconnected(object? sender, WebsocketDisconnectedArgs e)
    {
        _logger.LogWarning("EventSub WebSocket disconnected — attempting reconnect");

        CancellationToken token = _cts?.Token ?? CancellationToken.None;
        int attempt = 0;
        while (!token.IsCancellationRequested && !await _eventSub.ReconnectAsync())
        {
            attempt++;
            int delay = Math.Min(1000 * (int)Math.Pow(2, attempt), 30000);
            _logger.LogWarning("EventSub reconnect attempt {Attempt} failed — retrying in {Delay}ms",
                attempt, delay);

            try
            {
                await Task.Delay(delay, token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("EventSub reconnect cancelled — shutting down");
                return;
            }

            if (attempt >= 10)
            {
                _logger.LogError("EventSub reconnect failed after {Attempts} attempts — giving up", attempt);
                return;
            }
        }
    }

    private Task OnWebsocketReconnected(object? sender, WebsocketReconnectedArgs e)
    {
        _logger.LogInformation("EventSub WebSocket reconnected — session {SessionId}",
            _eventSub.SessionId);
        return Task.CompletedTask;
    }

    private Task OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        _logger.LogError(e.Exception, "EventSub error occurred");
        return Task.CompletedTask;
    }

    // ─── EventSub Subscription Registration ───────────────────────────

    private async Task SubscribeToEventsAsync()
    {
        if (_broadcasterId is null || _broadcasterAccessToken is null)
        {
            _logger.LogWarning("Cannot subscribe to EventSub — no broadcaster ID");
            return;
        }

        string sessionId = _eventSub.SessionId;
        string? clientId = await _storage.LoadClientIdAsync(CancellationToken.None);

        if (clientId is null)
        {
            _logger.LogError("Cannot subscribe to EventSub — no Client ID available");
            return;
        }

        await CreateSubscriptionAsync("channel.follow", "2",
            new { broadcaster_user_id = _broadcasterId, moderator_user_id = _broadcasterId },
            sessionId, clientId);

        await CreateSubscriptionAsync("channel.subscribe", "1",
            new { broadcaster_user_id = _broadcasterId },
            sessionId, clientId);

        await CreateSubscriptionAsync("channel.subscription.gift", "1",
            new { broadcaster_user_id = _broadcasterId },
            sessionId, clientId);

        await CreateSubscriptionAsync("channel.subscription.message", "1",
            new { broadcaster_user_id = _broadcasterId },
            sessionId, clientId);

        await CreateSubscriptionAsync("channel.raid", "1",
            new { to_broadcaster_user_id = _broadcasterId },
            sessionId, clientId);

        _logger.LogInformation("All EventSub subscriptions registered successfully");
    }

    private async Task CreateSubscriptionAsync(
        string type, string version, object condition,
        string sessionId, string clientId)
    {
        try
        {
            object body = new
            {
                type,
                version,
                condition,
                transport = new
                {
                    method = "websocket",
                    session_id = sessionId
                }
            };

            string json = JsonSerializer.Serialize(body);
            HttpRequestMessage request = new(HttpMethod.Post,
                "https://api.twitch.tv/helix/eventsub/subscriptions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _broadcasterAccessToken);
            request.Headers.Add("Client-Id", clientId);

            HttpResponseMessage response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("EventSub subscription created: {Type}", type);
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                string sanitizedError = error.Length > 200 ? error[..200] + "…" : error;
                _logger.LogWarning("EventSub subscription failed for {Type}: {Status} — {Error}",
                    type, response.StatusCode, sanitizedError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create EventSub subscription: {Type}", type);
        }
    }

    // ─── Event Handlers ───────────────────────────────────────────────

    private async Task OnChannelFollow(object? sender, ChannelFollowArgs e)
    {
        string username = e.Payload.Event.UserName;
        _logger.LogInformation("New follow: {Username}", username);

        await SendNotificationAsync("follow", new Dictionary<string, string>
        {
            { "user", username }
        });

        await _broadcaster.BroadcastFollowEventAsync(username);
    }

    private async Task OnChannelSubscribe(object? sender, ChannelSubscribeArgs e)
    {
        string username = e.Payload.Event.UserName;
        string tier = e.Payload.Event.Tier;
        int tierNumber = ParseTier(tier);

        _logger.LogInformation("New sub: {Username} (Tier {Tier})", username, tierNumber);

        await SendNotificationAsync("subscribe", new Dictionary<string, string>
        {
            { "user", username },
            { "tier", tierNumber.ToString() }
        });

        await _broadcaster.BroadcastSubscribeEventAsync(username, tierNumber);
    }

    private async Task OnChannelSubscriptionGift(object? sender, ChannelSubscriptionGiftArgs e)
    {
        string gifter = e.Payload.Event.UserName ?? "Anonymous";
        int total = e.Payload.Event.Total;
        string tier = e.Payload.Event.Tier;
        int tierNumber = ParseTier(tier);

        _logger.LogInformation("Gift subs: {Gifter} gifted {Count} Tier {Tier} subs",
            gifter, total, tierNumber);

        await SendNotificationAsync("gift", new Dictionary<string, string>
        {
            { "user", gifter },
            { "count", total.ToString() },
            { "tier", tierNumber.ToString() }
        });

        await _broadcaster.BroadcastGiftSubEventAsync(gifter, total, tierNumber);
    }

    private async Task OnChannelSubscriptionMessage(object? sender, ChannelSubscriptionMessageArgs e)
    {
        string username = e.Payload.Event.UserName;
        int months = e.Payload.Event.CumulativeMonths;
        string tier = e.Payload.Event.Tier;
        int tierNumber = ParseTier(tier);
        string? message = e.Payload.Event.Message?.Text;

        _logger.LogInformation("Resub: {Username} — {Months} months (Tier {Tier})",
            username, months, tierNumber);

        await SendNotificationAsync("resub", new Dictionary<string, string>
        {
            { "user", username },
            { "months", months.ToString() },
            { "tier", tierNumber.ToString() },
            { "message", message ?? "" }
        });

        await _broadcaster.BroadcastResubEventAsync(username, months, tierNumber, message);
    }

    private async Task OnChannelRaid(object? sender, ChannelRaidArgs e)
    {
        string raider = e.Payload.Event.FromBroadcasterUserName;
        int viewers = e.Payload.Event.Viewers;

        _logger.LogInformation("Raid: {Raider} with {Viewers} viewers", raider, viewers);

        await SendNotificationAsync("raid", new Dictionary<string, string>
        {
            { "user", raider },
            { "viewers", viewers.ToString() }
        });

        await _broadcaster.BroadcastRaidEventAsync(raider, viewers);

        // Auto-Shoutout check
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            string? autoSo = await settings.GetAsync("Notifications.raid.AutoShoutout");

            if (string.Equals(autoSo, "true", StringComparison.OrdinalIgnoreCase)
                && _broadcasterId is not null && _broadcasterAccessToken is not null)
            {
                string? raiderUserId = e.Payload.Event.FromBroadcasterUserId;
                if (raiderUserId is not null)
                {
                    await SendShoutoutViaHelixAsync(raiderUserId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-shoutout failed for raider {Raider}", raider);
        }
    }

    // ─── Notification Dispatch ────────────────────────────────────────

    private async Task SendNotificationAsync(string eventType, Dictionary<string, string> variables)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

            string enabledKey = $"Notifications.{eventType}.Enabled";
            string? enabled = await settings.GetAsync(enabledKey);
            if (string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string templateKey = $"Notifications.{eventType}.Template";
            string? template = await settings.GetAsync(templateKey);

            if (string.IsNullOrWhiteSpace(template))
            {
                template = GetDefaultTemplate(eventType);
            }

            string message = template;
            foreach (KeyValuePair<string, string> variable in variables)
            {
                message = message.Replace($"{{{variable.Key}}}", variable.Value);
            }

            await _chatClient.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {EventType} notification", eventType);
        }
    }

    private async Task SendShoutoutViaHelixAsync(string targetUserId)
    {
        try
        {
            string? clientId = await _storage.LoadClientIdAsync(CancellationToken.None);
            if (clientId is null || _broadcasterId is null || _broadcasterAccessToken is null)
            {
                return;
            }

            string url = $"https://api.twitch.tv/helix/chat/shoutouts" +
                         $"?from_broadcaster_id={_broadcasterId}" +
                         $"&to_broadcaster_id={targetUserId}" +
                         $"&moderator_id={_broadcasterId}";

            HttpRequestMessage request = new(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _broadcasterAccessToken);
            request.Headers.Add("Client-Id", clientId);

            HttpResponseMessage response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Auto-shoutout sent for user {UserId}", targetUserId);
            }
            else
            {
                _logger.LogDebug("Auto-shoutout failed: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send auto-shoutout");
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    internal static string GetDefaultTemplate(string eventType) => eventType switch
    {
        "follow" => "Welcome {user}! Thanks for the follow!",
        "subscribe" => "{user} just subscribed (Tier {tier})! Thank you!",
        "gift" => "{user} gifted {count} Tier {tier} subs! Amazing!",
        "resub" => "{user} resubscribed for {months} months (Tier {tier})! {message}",
        "raid" => "{user} is raiding with {viewers} viewers! Welcome raiders!",
        _ => ""
    };

    internal static int ParseTier(string tier) => tier switch
    {
        "2000" => 2,
        "3000" => 3,
        _ => 1
    };
}
