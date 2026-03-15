using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

public class SettingTests
{
    [Fact]
    public void Setting_DefaultValues_AreCorrect()
    {
        Setting setting = new();

        setting.Key.Should().BeEmpty();
        setting.Value.Should().BeEmpty();
    }

    [Fact]
    public void Setting_CanSetKeyAndValue()
    {
        Setting setting = new() { Key = "Bot.Channel", Value = "testchannel" };

        setting.Key.Should().Be("Bot.Channel");
        setting.Value.Should().Be("testchannel");
    }
}
