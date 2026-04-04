using System;
using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

/// <summary>Tests for the ChatMessage record type.</summary>
public class ChatMessageTests
{
    /// <summary>Verifies that ChatMessage properties are set correctly and Channel has a default empty value.</summary>
    [Fact]
    public void ChatMessage_IsImmutableRecord()
    {
        ChatMessage msg = new(
            UserId: "123",
            Username: "test",
            DisplayName: "Test",
            Content: "hello",
            IsModerator: false,
            IsSubscriber: false,
            IsBroadcaster: false,
            Timestamp: DateTimeOffset.UtcNow);

        msg.UserId.Should().Be("123");
        msg.Content.Should().Be("hello");
        msg.Channel.Should().BeEmpty(); // default value
    }

    /// <summary>Verifies that the Channel property can be set via object initializer.</summary>
    [Fact]
    public void ChatMessage_WithChannel_SetsChannel()
    {
        ChatMessage msg = new("123", "test", "Test", "hello", false, false, false, DateTimeOffset.UtcNow)
        {
            Channel = "mychannel"
        };

        msg.Channel.Should().Be("mychannel");
    }

    /// <summary>Verifies that two ChatMessage records with identical values are structurally equal.</summary>
    [Fact]
    public void ChatMessage_RecordEquality_WorksCorrectly()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ChatMessage msg1 = new("123", "test", "Test", "hello", false, false, false, now);
        ChatMessage msg2 = new("123", "test", "Test", "hello", false, false, false, now);

        msg1.Should().BeEquivalentTo(msg2);
    }

    /// <summary>Verifies that the with expression creates a new instance without modifying the original.</summary>
    [Fact]
    public void ChatMessage_WithExpression_CreatesNewInstance()
    {
        ChatMessage original = new("123", "test", "Test", "hello", false, false, false, DateTimeOffset.UtcNow);
        ChatMessage modified = original with { Content = "world" };

        modified.Content.Should().Be("world");
        original.Content.Should().Be("hello"); // original unchanged
    }
}
