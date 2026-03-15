using System;
using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

public class TwitchTokensTests
{
    [Fact]
    public void IsLikelyExpired_FreshToken_ReturnsFalse()
    {
        TwitchTokens tokens = new()
        {
            AccessToken = "abc",
            RefreshToken = "def",
            ExpiresIn = 14400, // 4 hours
            ObtainedAt = DateTimeOffset.UtcNow
        };

        tokens.IsLikelyExpired.Should().BeFalse();
    }

    [Fact]
    public void IsLikelyExpired_ExpiredToken_ReturnsTrue()
    {
        TwitchTokens tokens = new()
        {
            AccessToken = "abc",
            RefreshToken = "def",
            ExpiresIn = 3600, // 1 hour
            ObtainedAt = DateTimeOffset.UtcNow.AddHours(-2) // obtained 2 hours ago
        };

        tokens.IsLikelyExpired.Should().BeTrue();
    }

    [Fact]
    public void IsLikelyExpired_WithinSafetyMargin_ReturnsTrue()
    {
        // Token expires in 3 minutes — within the 5-minute safety margin
        TwitchTokens tokens = new()
        {
            AccessToken = "abc",
            RefreshToken = "def",
            ExpiresIn = 180, // 3 minutes
            ObtainedAt = DateTimeOffset.UtcNow
        };

        tokens.IsLikelyExpired.Should().BeTrue();
    }

    [Fact]
    public void IsLikelyExpired_JustOutsideSafetyMargin_ReturnsFalse()
    {
        // Token expires in 10 minutes — outside the 5-minute safety margin
        TwitchTokens tokens = new()
        {
            AccessToken = "abc",
            RefreshToken = "def",
            ExpiresIn = 600, // 10 minutes
            ObtainedAt = DateTimeOffset.UtcNow
        };

        tokens.IsLikelyExpired.Should().BeFalse();
    }

    [Fact]
    public void IsLikelyExpired_ZeroExpiresIn_ReturnsTrue()
    {
        TwitchTokens tokens = new()
        {
            AccessToken = "abc",
            ExpiresIn = 0,
            ObtainedAt = DateTimeOffset.UtcNow
        };

        tokens.IsLikelyExpired.Should().BeTrue();
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        TwitchTokens tokens = new();

        tokens.AccessToken.Should().BeEmpty();
        tokens.RefreshToken.Should().BeEmpty();
        tokens.ExpiresIn.Should().Be(0);
        tokens.Scope.Should().BeEmpty();
        tokens.TokenType.Should().Be("bearer");
    }
}
