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
    private bool _initialLoadDone;

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
            // If another caller already populated the cache while we were waiting
            // for the semaphore, skip the expensive Helix API calls.
            if (_cachedEmotes.Count > 0)
            {
                _logger.LogDebug(
                    "Emote cache already populated ({Count} emotes), skipping redundant refresh",
                    _cachedEmotes.Count);
                return;
            }

            await LoadEmotesAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Forces a full reload of the emote cache, even if already populated.
    /// Used by the 30-minute periodic timer to pick up newly added emotes.
    /// </summary>
    private async Task ForceRefreshAsync(CancellationToken ct = default)
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
            // First tick after startup: defer to RefreshAsync (skips when the
            // frontend has already populated the cache via /api/emotes/refresh).
            // Subsequent 30-min ticks force-reload to pick up newly added emotes.
            if (!_initialLoadDone)
            {
                _initialLoadDone = true;
                await RefreshAsync();
            }
            else
            {
                await ForceRefreshAsync();
            }

            // Retry after 30s if cache is still empty (typical at startup when tokens aren't ready yet)
            if (_cachedEmotes.Count == 0)
            {
                _logger.LogInformation("Emote cache still empty after refresh — scheduling retry in 30s");
                _ = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(async _ =>
                {
                    try
                    {
                        await ForceRefreshAsync();
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

        // Dedup by emote ID. When the same emote is loaded by both bot and broadcaster
        // (e.g. both subscribe to the same channel), the entry is upgraded to Owner="shared"
        // so the EmotePicker shows it regardless of which account is selected.
        Dictionary<string, EmoteDto> emoteMap = new(System.StringComparer.Ordinal);

        TwitchTokens? botTokens = await secureStorage.LoadTokensAsync(TokenType.Bot, ct);
        TwitchTokens? broadcasterTokens = await secureStorage.LoadTokensAsync(TokenType.Broadcaster, ct);

        _logger.LogDebug("Emote load starting — Bot token: {BotPresent}, Broadcaster token: {BroadcasterPresent}",
            botTokens is not null, broadcasterTokens is not null);

        bool userEmotesLoaded = false;

        // === Strategy 1: User Emotes API (preferred — returns ALL emotes a user can use) ===
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
                            UpsertEmote(emoteMap, emote, MapEmoteTypeToSource(emote.EmoteType), owner: "bot");
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
                        foreach (TwitchEmote emote in broadcasterUserEmotes)
                        {
                            UpsertEmote(emoteMap, emote, MapEmoteTypeToSource(emote.EmoteType), owner: "broadcaster");
                        }
                        _logger.LogDebug("Loaded {Count} emotes via Broadcaster client", broadcasterUserEmotes.Count);
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

            bool globalLoaded = false;

            if (botTokens is not null)
            {
                try
                {
                    IBotHelixClient botHelix = scope.ServiceProvider.GetRequiredService<IBotHelixClient>();
                    IReadOnlyList<TwitchEmote> globalEmotes = await botHelix.GetGlobalEmotesAsync(ct);
                    foreach (TwitchEmote emote in globalEmotes)
                    {
                        // Global emotes are usable by every account → always shared.
                        UpsertEmote(emoteMap, emote, "global", owner: "shared");
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
                        UpsertEmote(emoteMap, emote, "global", owner: "shared");
                    }
                    _logger.LogDebug("Fallback: Loaded {Count} global emotes via Broadcaster client", globalEmotes.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fallback: Failed to load global emotes via Broadcaster client");
                }
            }

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
                            UpsertEmote(emoteMap, emote, MapEmoteTypeToSource(emote.EmoteType), owner: "broadcaster");
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

        if (emoteMap.Count == 0 && botTokens is null && broadcasterTokens is null)
        {
            _logger.LogWarning("No authenticated client available for emotes — skipping");
        }

        List<EmoteDto> emotes = emoteMap.Values.ToList();
        _cachedEmotes = emotes;

        int globalCount = emotes.Count(e => e.Source == "global");
        int subCount = emotes.Count(e => e.Source == "subscriber");
        int bitsCount = emotes.Count(e => e.Source == "bits");
        int followerCount = emotes.Count(e => e.Source == "follower");
        int otherCount = emotes.Count - globalCount - subCount - bitsCount - followerCount;
        int botOwned = emotes.Count(e => e.Owner == "bot");
        int broadcasterOwned = emotes.Count(e => e.Owner == "broadcaster");
        int sharedOwned = emotes.Count(e => e.Owner == "shared");

        _logger.LogInformation(
            "Emote cache refreshed: {Total} total (global: {Global}, sub: {Sub}, bits: {Bits}, follower: {Follower}, other: {Other}) | owners — bot: {Bot}, broadcaster: {Broadcaster}, shared: {Shared}",
            emotes.Count, globalCount, subCount, bitsCount, followerCount, otherCount, botOwned, broadcasterOwned, sharedOwned);
    }

    /// <summary>
    /// Inserts an emote into the dedup map, or upgrades an existing entry's Owner to "shared"
    /// when the same emote is loaded by both bot and broadcaster (or as a global).
    /// </summary>
    private static void UpsertEmote(Dictionary<string, EmoteDto> map, TwitchEmote emote, string source, string owner)
    {
        if (string.IsNullOrEmpty(emote.Id))
        {
            return;
        }

        string url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/2.0";

        if (map.TryGetValue(emote.Id, out EmoteDto? existing))
        {
            // If the existing owner differs from the incoming one, the emote is usable by
            // both accounts → upgrade to shared. "shared" beats everything else.
            if (existing.Owner != owner && existing.Owner != "shared")
            {
                map[emote.Id] = new EmoteDto
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    Url = existing.Url,
                    Source = existing.Source,
                    Owner = "shared",
                };
            }
            return;
        }

        map[emote.Id] = new EmoteDto
        {
            Id = emote.Id,
            Name = emote.Name,
            Url = url,
            Source = source,
            Owner = owner,
        };
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
