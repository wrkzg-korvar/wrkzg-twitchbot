using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

/// <summary>Tests for the Setting model.</summary>
public class SettingTests
{
    /// <summary>Verifies that a new Setting has empty default values for Key and Value.</summary>
    [Fact]
    public void Setting_DefaultValues_AreCorrect()
    {
        Setting setting = new();

        setting.Key.Should().BeEmpty();
        setting.Value.Should().BeEmpty();
    }

    /// <summary>Verifies that Key and Value can be set via object initializer.</summary>
    [Fact]
    public void Setting_CanSetKeyAndValue()
    {
        Setting setting = new() { Key = "Bot.Channel", Value = "testchannel" };

        setting.Key.Should().Be("Bot.Channel");
        setting.Value.Should().Be("testchannel");
    }
}
