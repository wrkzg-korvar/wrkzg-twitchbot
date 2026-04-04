using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;
using Xunit;

namespace Wrkzg.Core.Tests.Services;

/// <summary>Tests for the SpamFilterService including link, caps, banned word, and exemption filters.</summary>
public class SpamFilterServiceTests
{
    private readonly ISettingsRepository _settings;
    private readonly ITwitchChatClient _chatClient;
    private readonly ITwitchHelixClient _helix;
    private readonly SpamFilterService _sut;

    /// <summary>Initializes test dependencies with NSubstitute mocks.</summary>
    public SpamFilterServiceTests()
    {
        _settings = Substitute.For<ISettingsRepository>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        _helix = Substitute.For<ITwitchHelixClient>();
        ILogger<SpamFilterService> logger = Substitute.For<ILogger<SpamFilterService>>();

        // Default: all filters enabled via default config (no settings overrides)
        _settings.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        _sut = new SpamFilterService(_settings, _chatClient, _helix, logger);
    }

    private static ChatMessage Msg(string content, bool isMod = false, bool isSub = false, bool isBroadcaster = false)
    {
        return new ChatMessage("123", "testuser", "TestUser", content, isMod, isSub, isBroadcaster, DateTimeOffset.UtcNow);
    }

    /// <summary>Verifies that the link filter blocks messages containing URLs.</summary>
    [Fact]
    public async Task LinksFilter_BlocksUrl()
    {
        bool result = await _sut.CheckAsync(Msg("check out http://evil.com"));

        result.Should().BeTrue();
        await _chatClient.Received(1).SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that whitelisted domains like clips.twitch.tv are allowed.</summary>
    [Fact]
    public async Task LinksFilter_AllowsWhitelistedDomain()
    {
        bool result = await _sut.CheckAsync(Msg("check https://clips.twitch.tv/SomeClip"));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that subscribers are exempt from the link filter.</summary>
    [Fact]
    public async Task LinksFilter_ExemptSubscriber()
    {
        bool result = await _sut.CheckAsync(Msg("http://example.com", isSub: true));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that the caps filter blocks messages with excessive uppercase.</summary>
    [Fact]
    public async Task CapsFilter_BlocksExcessiveCaps()
    {
        bool result = await _sut.CheckAsync(Msg("THIS IS ALL CAPS MESSAGE HERE"));

        result.Should().BeTrue();
    }

    /// <summary>Verifies that the caps filter ignores short messages below the minimum length.</summary>
    [Fact]
    public async Task CapsFilter_IgnoresShortMessages()
    {
        bool result = await _sut.CheckAsync(Msg("OK"));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that messages containing banned words are blocked.</summary>
    [Fact]
    public async Task BannedWords_BlocksBannedWord()
    {
        _settings.GetAsync("spam.banned.words", Arg.Any<CancellationToken>()).Returns("badword,terrible");

        bool result = await _sut.CheckAsync(Msg("you are a badword"));

        result.Should().BeTrue();
    }

    /// <summary>Verifies that banned word matching is case-insensitive.</summary>
    [Fact]
    public async Task BannedWords_CaseInsensitive()
    {
        _settings.GetAsync("spam.banned.words", Arg.Any<CancellationToken>()).Returns("BadWord");

        bool result = await _sut.CheckAsync(Msg("you are a BADWORD"));

        result.Should().BeTrue();
    }

    /// <summary>Verifies that the broadcaster is exempt from all spam filters.</summary>
    [Fact]
    public async Task Broadcaster_AlwaysExempt()
    {
        bool result = await _sut.CheckAsync(Msg("http://evil.com CAPS CAPS", isBroadcaster: true));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that moderators are exempt from the link filter.</summary>
    [Fact]
    public async Task Mod_ExemptFromLinks()
    {
        bool result = await _sut.CheckAsync(Msg("http://evil.com", isMod: true));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that links are allowed when the link filter is disabled.</summary>
    [Fact]
    public async Task LinksFilter_Disabled_AllowsLinks()
    {
        _settings.GetAsync("spam.links.enabled", Arg.Any<CancellationToken>()).Returns("False");

        bool result = await _sut.CheckAsync(Msg("http://evil.com"));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that a normal message is not flagged as spam.</summary>
    [Fact]
    public async Task ReturnsNotSpam_WhenNothingMatches()
    {
        bool result = await _sut.CheckAsync(Msg("just a normal chat message hello everyone"));

        result.Should().BeFalse();
    }

    /// <summary>Verifies that excessive caps are allowed when the caps filter is disabled.</summary>
    [Fact]
    public async Task CapsFilter_Disabled_AllowsCaps()
    {
        _settings.GetAsync("spam.caps.enabled", Arg.Any<CancellationToken>()).Returns("False");

        bool result = await _sut.CheckAsync(Msg("THIS IS ALL CAPS MESSAGE HERE"));

        result.Should().BeFalse();
    }
}
