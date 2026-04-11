using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Services;

/// <summary>
/// Background service that periodically fetches and caches Twitch emotes
/// (global + channel) from the Helix API. Refreshes every 30 minutes.
/// </summary>
public class EmoteService : IHostedService, IEmoteService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmoteService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private volatile IReadOnlyList<EmoteDto> _cachedEmotes = Array.Empty<EmoteDto>();
    private Timer? _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmoteService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers to access Helix client.</param>
    /// <param name="logger">The logger for emote service diagnostics.</param>
    public EmoteService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmoteService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<EmoteDto> GetCachedEmotes()
    {
        return _cachedEmotes;
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!await _refreshLock.WaitAsync(TimeSpan.FromSeconds(5), ct))
        {
            _logger.LogDebug("Emote refresh already in progress, skipping");
            return;
        }

        try
        {
            await LoadEmotesAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Starts the emote refresh timer with an initial 10-second delay.</summary>
    public Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("EmoteService starting — initial load in 10s, then every 30min");
        _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(30));
        return Task.CompletedTask;
    }

    /// <summary>Stops the emote refresh timer.</summary>
    public Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("EmoteService stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>Disposes the refresh timer and semaphore.</summary>
    public void Dispose()
    {
        _timer?.Dispose();
        _refreshLock.Dispose();
    }

    private async void OnTimerTick(object? state)
    {
        try
        {
            await RefreshAsync();

            // Retry after 30s if cache is still empty (typical at startup when tokens aren't ready yet)
            if (_cachedEmotes.Count == 0)
            {
                _logger.LogInformation("Emote cache still empty after refresh — scheduling retry in 30s");
                _ = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(async _ =>
                {
                    try
                    {
                        await RefreshAsync();
                        if (_cachedEmotes.Count > 0)
                        {
                            _logger.LogInformation("Emote retry successful — {Count} emotes loaded", _cachedEmotes.Count);
                        }
                        else
                        {
                            _logger.LogWarning("Emote retry still returned 0 emotes — next attempt in 30min or on auth change");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogWarning(retryEx, "Emote retry failed");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Emote refresh timer tick failed");
        }
    }

    private async Task LoadEmotesAsync(CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISecureStorage secureStorage = scope.ServiceProvider.GetRequiredService<ISecureStorage>();
        ITwitchOAuthService oauthService = scope.ServiceProvider.GetRequiredService<ITwitchOAuthService>();

        List<EmoteDto> emotes = new();
        HashSet<string> seenIds = new();

        TwitchTokens? botTokens = await secureStorage.LoadTokensAsync(TokenType.Bot, ct);
        TwitchTokens? broadcasterTokens = await secureStorage.LoadTokensAsync(TokenType.Broadcaster, ct);

        _logger.LogDebug("Emote load starting — Bot token: {BotPresent}, Broadcaster token: {BroadcasterPresent}",
            botTokens is not null, broadcasterTokens is not null);

        bool userEmotesLoaded = false;

        // === Strategy 1: User Emotes API (preferred — returns ALL emotes a user can use) ===
        // Try Bot user first, then Broadcaster
        if (botTokens is not null)
        {
            try
            {
                TwitchTokenValidation? botValidation = await oauthService.ValidateTokenAsync(botTokens.AccessToken, ct);
                if (botValidation is not null)
                {
                    IBotHelixClient botHelix = scope.ServiceProvider.GetRequiredService<IBotHelixClient>();
                    IReadOnlyList<TwitchEmote> botUserEmotes = await botHelix.GetUserEmotesAsync(botValidation.UserId, ct);

                    if (botUserEmotes.Count > 0)
                    {
                        foreach (TwitchEmote emote in botUserEmotes)
                        {
                            if (seenIds.Add(emote.Id))
                            {
                                emotes.Add(new EmoteDto
                                {
                                    Id = emote.Id,
                                    Name = emote.Name,
                                    Url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0",
                                    Source = MapEmoteTypeToSource(emote.EmoteType)
                                });
                            }
                        }
                        _logger.LogDebug("Loaded {Count} user emotes via Bot client", botUserEmotes.Count);
                        userEmotesLoaded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user emotes via Bot client — will try fallback");
            }
        }

        // Add Broadcaster user emotes (may have different subscriptions)
        if (broadcasterTokens is not null)
        {
            try
            {
                TwitchTokenValidation? validation = await oauthService.ValidateTokenAsync(broadcasterTokens.AccessToken, ct);
                if (validation is not null)
                {
                    IBroadcasterHelixClient broadcasterHelix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
                    IReadOnlyList<TwitchEmote> broadcasterUserEmotes = await broadcasterHelix.GetUserEmotesAsync(validation.UserId, ct);

                    if (broadcasterUserEmotes.Count > 0)
                    {
                        int added = 0;
                        foreach (TwitchEmote emote in broadcasterUserEmotes)
                        {
                            if (seenIds.Add(emote.Id))
                            {
                                emotes.Add(new EmoteDto
                                {
                                    Id = emote.Id,
                                    Name = emote.Name,
                                    Url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0",
                                    Source = MapEmoteTypeToSource(emote.EmoteType)
                                });
                                added++;
                            }
                        }
                        _logger.LogDebug("Loaded {Count} additional emotes via Broadcaster client ({Total} total from broadcaster, {Added} new)",
                            broadcasterUserEmotes.Count, broadcasterUserEmotes.Count, added);
                        userEmotesLoaded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user emotes via Broadcaster client");
            }
        }

        // === Strategy 2: Fallback — Global + Channel emotes (old tokens without user:read:emotes) ===
        if (!userEmotesLoaded)
        {
            _logger.LogInformation("User emotes API unavailable — falling back to global + channel emotes. " +
                                   "Re-connect Bot and Broadcaster accounts in Settings to enable user:read:emotes scope.");

            // Global emotes via Bot or Broadcaster
            bool globalLoaded = false;

            if (botTokens is not null)
            {
                try
                {
                    IBotHelixClient botHelix = scope.ServiceProvider.GetRequiredService<IBotHelixClient>();
                    IReadOnlyList<TwitchEmote> globalEmotes = await botHelix.GetGlobalEmotesAsync(ct);
                    foreach (TwitchEmote emote in globalEmotes)
                    {
                        if (seenIds.Add(emote.Id))
                        {
                            emotes.Add(new EmoteDto
                            {
                                Id = emote.Id,
                                Name = emote.Name,
                                Url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0",
                                Source = "global"
                            });
                        }
                    }
                    _logger.LogDebug("Fallback: Loaded {Count} global emotes via Bot client", globalEmotes.Count);
                    globalLoaded = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fallback: Failed to load global emotes via Bot client");
                }
            }

            if (!globalLoaded && broadcasterTokens is not null)
            {
                try
                {
                    IBroadcasterHelixClient broadcasterHelix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
                    IReadOnlyList<TwitchEmote> globalEmotes = await broadcasterHelix.GetGlobalEmotesAsync(ct);
                    foreach (TwitchEmote emote in globalEmotes)
                    {
                        if (seenIds.Add(emote.Id))
                        {
                            emotes.Add(new EmoteDto
                            {
                                Id = emote.Id,
                                Name = emote.Name,
                                Url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0",
                                Source = "global"
                            });
                        }
                    }
                    _logger.LogDebug("Fallback: Loaded {Count} global emotes via Broadcaster client", globalEmotes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fallback: Failed to load global emotes via Broadcaster client");
                }
            }

            // Channel emotes
            if (broadcasterTokens is not null)
            {
                try
                {
                    TwitchTokenValidation? validation = await oauthService.ValidateTokenAsync(broadcasterTokens.AccessToken, ct);
                    if (validation is not null)
                    {
                        IBroadcasterHelixClient broadcasterHelix = scope.ServiceProvider.GetRequiredService<IBroadcasterHelixClient>();
                        IReadOnlyList<TwitchEmote> channelEmotes = await broadcasterHelix.GetChannelEmotesAsync(validation.UserId, ct);
                        foreach (TwitchEmote emote in channelEmotes)
                        {
                            if (seenIds.Add(emote.Id))
                            {
                                emotes.Add(new EmoteDto
                                {
                                    Id = emote.Id,
                                    Name = emote.Name,
                                    Url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0",
                                    Source = MapEmoteTypeToSource(emote.EmoteType)
                                });
                            }
                        }
                        _logger.LogDebug("Fallback: Loaded {Count} channel emotes", channelEmotes.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fallback: Failed to load channel emotes");
                }
            }
        }

        if (emotes.Count == 0 && botTokens is null && broadcasterTokens is null)
        {
            _logger.LogWarning("No authenticated client available for emotes — skipping");
        }

        _cachedEmotes = emotes;

        int globalCount = emotes.Count(e => e.Source == "global");
        int subCount = emotes.Count(e => e.Source == "subscriber");
        int bitsCount = emotes.Count(e => e.Source == "bits");
        int followerCount = emotes.Count(e => e.Source == "follower");
        int otherCount = emotes.Count - globalCount - subCount - bitsCount - followerCount;

        _logger.LogInformation("Emote cache refreshed: {Total} total emotes (global: {Global}, subscriber: {Sub}, bits: {Bits}, follower: {Follower}, other: {Other})",
            emotes.Count, globalCount, subCount, bitsCount, followerCount, otherCount);
    }

    private static string MapEmoteTypeToSource(string emoteType)
    {
        return emoteType switch
        {
            "globals" or "smilies" or "limitedtime" => "global",
            "subscriptions" => "subscriber",
            "bitstier" => "bits",
            "follower" => "follower",
            "channelpoints" => "channel",
            _ => "channel"
        };
    }
}
