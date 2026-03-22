using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for spam filter configuration.
/// </summary>
public static class SpamFilterEndpoints
{
    public static void MapSpamFilterEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/spam-filter").WithTags("SpamFilter");

        group.MapGet("/", async (ISettingsRepository settings, CancellationToken ct) =>
        {
            SpamFilterConfig config = await LoadConfigAsync(settings, ct);
            return Results.Ok(config);
        });

        group.MapPut("/", async (SpamFilterConfig config, ISettingsRepository settings, CancellationToken ct) =>
        {
            Dictionary<string, string> values = new()
            {
                ["spam.links.enabled"] = config.LinksEnabled.ToString(),
                ["spam.links.timeout"] = config.LinksTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                ["spam.links.subs_exempt"] = config.LinksSubsExempt.ToString(),
                ["spam.links.mods_exempt"] = config.LinksModsExempt.ToString(),
                ["spam.links.whitelist"] = config.LinkWhitelist,
                ["spam.caps.enabled"] = config.CapsEnabled.ToString(),
                ["spam.caps.min_length"] = config.CapsMinLength.ToString(CultureInfo.InvariantCulture),
                ["spam.caps.max_percent"] = config.CapsMaxPercent.ToString(CultureInfo.InvariantCulture),
                ["spam.caps.timeout"] = config.CapsTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                ["spam.caps.subs_exempt"] = config.CapsSubsExempt.ToString(),
                ["spam.banned.enabled"] = config.BannedWordsEnabled.ToString(),
                ["spam.banned.words"] = config.BannedWordsList,
                ["spam.banned.timeout"] = config.BannedWordsTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                ["spam.banned.subs_exempt"] = config.BannedWordsSubsExempt.ToString(),
                ["spam.emote.enabled"] = config.EmoteSpamEnabled.ToString(),
                ["spam.emote.max_emotes"] = config.EmoteSpamMaxEmotes.ToString(CultureInfo.InvariantCulture),
                ["spam.emote.timeout"] = config.EmoteSpamTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                ["spam.emote.subs_exempt"] = config.EmoteSpamSubsExempt.ToString(),
                ["spam.repeat.enabled"] = config.RepeatEnabled.ToString(),
                ["spam.repeat.max_count"] = config.RepeatMaxCount.ToString(CultureInfo.InvariantCulture),
                ["spam.repeat.timeout"] = config.RepeatTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                ["spam.repeat.subs_exempt"] = config.RepeatSubsExempt.ToString(),
            };
            await settings.SetManyAsync(values, ct);
            return Results.Ok(config);
        });
    }

    private static async Task<SpamFilterConfig> LoadConfigAsync(ISettingsRepository settings, CancellationToken ct)
    {
        IDictionary<string, string> all = await settings.GetAllAsync(ct);
        SpamFilterConfig config = new();

        if (all.TryGetValue("spam.links.enabled", out string? v1) && bool.TryParse(v1, out bool b1)) config.LinksEnabled = b1;
        if (all.TryGetValue("spam.links.timeout", out string? v2) && int.TryParse(v2, out int i2)) config.LinksTimeoutSeconds = i2;
        if (all.TryGetValue("spam.links.subs_exempt", out string? v3) && bool.TryParse(v3, out bool b3)) config.LinksSubsExempt = b3;
        if (all.TryGetValue("spam.links.mods_exempt", out string? vm1) && bool.TryParse(vm1, out bool bm1)) config.LinksModsExempt = bm1;
        if (all.TryGetValue("spam.links.whitelist", out string? v4)) config.LinkWhitelist = v4;

        if (all.TryGetValue("spam.caps.enabled", out string? v5) && bool.TryParse(v5, out bool b5)) config.CapsEnabled = b5;
        if (all.TryGetValue("spam.caps.min_length", out string? v6) && int.TryParse(v6, out int i6)) config.CapsMinLength = i6;
        if (all.TryGetValue("spam.caps.max_percent", out string? v7) && int.TryParse(v7, out int i7)) config.CapsMaxPercent = i7;
        if (all.TryGetValue("spam.caps.timeout", out string? v8) && int.TryParse(v8, out int i8)) config.CapsTimeoutSeconds = i8;
        if (all.TryGetValue("spam.caps.subs_exempt", out string? v9) && bool.TryParse(v9, out bool b9)) config.CapsSubsExempt = b9;

        if (all.TryGetValue("spam.banned.enabled", out string? v10) && bool.TryParse(v10, out bool b10)) config.BannedWordsEnabled = b10;
        if (all.TryGetValue("spam.banned.words", out string? v11)) config.BannedWordsList = v11;
        if (all.TryGetValue("spam.banned.timeout", out string? v12) && int.TryParse(v12, out int i12)) config.BannedWordsTimeoutSeconds = i12;
        if (all.TryGetValue("spam.banned.subs_exempt", out string? v13) && bool.TryParse(v13, out bool b13)) config.BannedWordsSubsExempt = b13;

        if (all.TryGetValue("spam.emote.enabled", out string? ve1) && bool.TryParse(ve1, out bool be1)) config.EmoteSpamEnabled = be1;
        if (all.TryGetValue("spam.emote.max_emotes", out string? ve2) && int.TryParse(ve2, out int ie2)) config.EmoteSpamMaxEmotes = ie2;
        if (all.TryGetValue("spam.emote.timeout", out string? ve3) && int.TryParse(ve3, out int ie3)) config.EmoteSpamTimeoutSeconds = ie3;
        if (all.TryGetValue("spam.emote.subs_exempt", out string? ve4) && bool.TryParse(ve4, out bool be4)) config.EmoteSpamSubsExempt = be4;

        if (all.TryGetValue("spam.repeat.enabled", out string? v14) && bool.TryParse(v14, out bool b14)) config.RepeatEnabled = b14;
        if (all.TryGetValue("spam.repeat.max_count", out string? v15) && int.TryParse(v15, out int i15)) config.RepeatMaxCount = i15;
        if (all.TryGetValue("spam.repeat.timeout", out string? v16) && int.TryParse(v16, out int i16)) config.RepeatTimeoutSeconds = i16;
        if (all.TryGetValue("spam.repeat.subs_exempt", out string? v17) && bool.TryParse(v17, out bool b17)) config.RepeatSubsExempt = b17;

        return config;
    }
}
