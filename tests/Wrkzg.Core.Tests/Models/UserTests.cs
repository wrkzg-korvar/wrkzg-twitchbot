using System;
using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

/// <summary>Tests for the User model default values and initialization.</summary>
public class UserTests
{
    /// <summary>Verifies that a new User has correct default property values.</summary>
    [Fact]
    public void User_DefaultValues_AreCorrect()
    {
        User user = new();

        user.Id.Should().Be(0);
        user.TwitchId.Should().BeEmpty();
        user.Username.Should().BeEmpty();
        user.DisplayName.Should().BeEmpty();
        user.Points.Should().Be(0);
        user.WatchedMinutes.Should().Be(0);
        user.MessageCount.Should().Be(0);
        user.FollowDate.Should().BeNull();
        user.IsSubscriber.Should().BeFalse();
        user.SubscriberTier.Should().Be(0);
        user.IsBanned.Should().BeFalse();
        user.IsMod.Should().BeFalse();
    }

    /// <summary>Verifies that navigation collection properties are initialized as empty lists.</summary>
    [Fact]
    public void User_NavigationProperties_AreInitialized()
    {
        User user = new();

        user.RaffleEntries.Should().NotBeNull().And.BeEmpty();
        user.PollVotes.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>Verifies that FirstSeenAt and LastSeenAt are initialized to the current time.</summary>
    [Fact]
    public void User_FirstSeenAt_IsSetToNow()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;
        User user = new();
        DateTimeOffset after = DateTimeOffset.UtcNow;

        user.FirstSeenAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        user.LastSeenAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
