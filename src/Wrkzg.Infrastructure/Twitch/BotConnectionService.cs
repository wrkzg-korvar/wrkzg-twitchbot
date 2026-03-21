using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// Background service that manages the bot's IRC connection lifecycle.
///
/// On startup: checks if Bot token + channel are configured, connects if available.
/// On shutdown: disconnects gracefully.
///
/// Uses IServiceScopeFactory to resolve Scoped dependencies (ISettingsRepository)
/// from within this Singleton-lifetime service.
/// </summary>
public class BotConnectionService : IHostedService, IBotConnectionService
{
    private readonly ITwitchChatClient _chatClient;
    private readonly ISecureStorage _storage;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ChatMessagePipeline _pipeline;
    private readonly ILogger<BotConnectionService> _logger;

    public BotConnectionService(
        ITwitchChatClient chatClient,
        ISecureStorage storage,
        IServiceScopeFactory scopeFactory,
        IChatEventBroadcaster broadcaster,
        ChatMessagePipeline pipeline,
        ILogger<BotConnectionService> logger)
    {
        _chatClient = chatClient;
        _storage = storage;
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("BotConnectionService starting");

        // Wire up chat client events → SignalR broadcaster
        _chatClient.OnConnected += HandleBotConnected;
        _chatClient.OnDisconnected += HandleBotDisconnected;
        _chatClient.OnMessageReceived += HandleChatMessage;

        // Try auto-connect if tokens and channel are configured
        await TryConnectAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("BotConnectionService stopping — disconnecting from IRC");

        _chatClient.OnConnected -= HandleBotConnected;
        _chatClient.OnDisconnected -= HandleBotDisconnected;
        _chatClient.OnMessageReceived -= HandleChatMessage;

        await _chatClient.DisconnectAsync(ct);
    }

    public async Task<bool> TryConnectAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if credentials are configured at all
            bool hasCredentials = await _storage.HasCredentialsAsync(ct);
            if (!hasCredentials)
            {
                _logger.LogInformation(
                    "No Twitch app credentials stored — skipping connect. " +
                    "Please complete the Setup Wizard.");
                return false;
            }

            // Check if Bot token exists
            TwitchTokens? botToken = await _storage.LoadTokensAsync(TokenType.Bot, ct);
            if (botToken is null)
            {
                _logger.LogInformation(
                    "No Bot token stored — skipping connect. " +
                    "Connect your bot account in the Settings page.");
                return false;
            }

            // Read channel from Settings (Scoped dependency → use scope factory)
            string? channel;
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                ISettingsRepository settings =
                    scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
                channel = await settings.GetAsync("Bot.Channel", ct);
            }

            if (string.IsNullOrWhiteSpace(channel))
            {
                _logger.LogInformation(
                    "Bot.Channel not configured — skipping connect. " +
                    "Set your channel name in the Settings page.");
                return false;
            }

            _logger.LogInformation("Connecting bot to channel #{Channel}", channel);
            await _chatClient.ConnectAsync(channel, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Connect failed — the bot will not be in chat. " +
                "Check your bot token and channel settings.");
            return false;
        }
    }

    // ─── Event Handlers ───────────────────────────────────────────────

    private async Task HandleBotConnected()
    {
        try
        {
            await _broadcaster.BroadcastBotStatusAsync(new BotStatus
            {
                IsConnected = true,
                Channel = _chatClient.JoinedChannel
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting bot connected status");
        }
    }

    private async Task HandleBotDisconnected()
    {
        try
        {
            await _broadcaster.BroadcastBotStatusAsync(new BotStatus
            {
                IsConnected = false,
                Channel = null,
                Reason = "Disconnected"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting bot disconnected status");
        }
    }

    private async Task HandleChatMessage(ChatMessage message)
    {
        try
        {
            // 1. Broadcast to dashboard (parallel — don't block pipeline)
            _ = _broadcaster.BroadcastChatMessageAsync(message);

            // 2. Process through command/game pipeline
            await _pipeline.ProcessAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling chat message from {User}", message.Username);
        }
    }
}
