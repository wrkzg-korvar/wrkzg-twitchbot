using System;
using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

/// <summary>Tests for the Command model and PermissionLevel enum.</summary>
public class CommandTests
{
    /// <summary>Verifies that a new Command has correct default property values.</summary>
    [Fact]
    public void Command_DefaultValues_AreCorrect()
    {
        Command cmd = new();

        cmd.Id.Should().Be(0);
        cmd.Trigger.Should().BeEmpty();
        cmd.Aliases.Should().BeEmpty();
        cmd.ResponseTemplate.Should().BeEmpty();
        cmd.PermissionLevel.Should().Be(PermissionLevel.Everyone);
        cmd.GlobalCooldownSeconds.Should().Be(0);
        cmd.UserCooldownSeconds.Should().Be(0);
        cmd.IsEnabled.Should().BeTrue();
        cmd.UseCount.Should().Be(0);
    }

    /// <summary>Verifies that permission levels are ordered from Everyone (lowest) to Broadcaster (highest).</summary>
    [Fact]
    public void PermissionLevel_Ordering_IsCorrect()
    {
        // Broadcaster should be the highest level
        ((int)PermissionLevel.Broadcaster).Should().BeGreaterThan((int)PermissionLevel.Moderator);
        ((int)PermissionLevel.Moderator).Should().BeGreaterThan((int)PermissionLevel.Subscriber);
        ((int)PermissionLevel.Subscriber).Should().BeGreaterThan((int)PermissionLevel.Follower);
        ((int)PermissionLevel.Follower).Should().BeGreaterThan((int)PermissionLevel.Everyone);
    }

    /// <summary>Verifies that PermissionLevel integer values are sequential starting from zero.</summary>
    [Fact]
    public void PermissionLevel_IntValues_AreSequential()
    {
        ((int)PermissionLevel.Everyone).Should().Be(0);
        ((int)PermissionLevel.Follower).Should().Be(1);
        ((int)PermissionLevel.Subscriber).Should().Be(2);
        ((int)PermissionLevel.Moderator).Should().Be(3);
        ((int)PermissionLevel.Broadcaster).Should().Be(4);
    }
}
