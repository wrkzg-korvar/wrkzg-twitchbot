using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;
using Xunit;

namespace Wrkzg.Core.Tests.Services;

/// <summary>Tests for the UserStatsBatcher service.</summary>
public class UserStatsBatcherTests
{
    private readonly IUserRepository _userRepo;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserStatsBatcher _sut;

    /// <summary>Initializes the test fixture.</summary>
    public UserStatsBatcherTests()
    {
        _userRepo = Substitute.For<IUserRepository>();

        ServiceCollection services = new();
        services.AddScoped(_ => _userRepo);
        ServiceProvider provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _sut = new UserStatsBatcher(_scopeFactory, Substitute.For<ILogger<UserStatsBatcher>>());
    }

    /// <summary>Enqueueing a single message does not throw and does not write immediately.</summary>
    [Fact]
    public async Task Enqueue_SingleMessage_DoesNotWriteImmediately()
    {
        _sut.Enqueue(BuildMessage("user1"));

        await _userRepo.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, default!, default);
        await _userRepo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    /// <summary>Enqueueing multiple messages from the same user accumulates the message count in a single update.</summary>
    [Fact]
    public async Task Flush_MergesMultipleMessagesPerUser()
    {
        ChatMessage msg1 = BuildMessage("user1", "TestUser", isMod: false);
        ChatMessage msg2 = BuildMessage("user1", "TestUser", isMod: true);
        ChatMessage msg3 = BuildMessage("user1", "TestUser", isMod: true);

        User user = new() { TwitchId = "user1", Username = "user1", MessageCount = 5 };
        _userRepo.GetOrCreateAsync("user1", "user1", Arg.Any<CancellationToken>()).Returns(user);

        _sut.Enqueue(msg1);
        _sut.Enqueue(msg2);
        _sut.Enqueue(msg3);

        await InvokeFlushAsync();

        // Three messages → MessageCount goes from 5 to 8 in a SINGLE UpdateAsync call.
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        user.MessageCount.Should().Be(8);
        user.IsMod.Should().BeTrue("the latest message had IsMod = true");
    }

    /// <summary>A single user with one message produces exactly one DB update.</summary>
    [Fact]
    public async Task Flush_WritesOncePerUser()
    {
        User userA = new() { TwitchId = "userA", Username = "alice", MessageCount = 0 };
        User userB = new() { TwitchId = "userB", Username = "bob", MessageCount = 0 };
        _userRepo.GetOrCreateAsync("userA", "alice", Arg.Any<CancellationToken>()).Returns(userA);
        _userRepo.GetOrCreateAsync("userB", "bob", Arg.Any<CancellationToken>()).Returns(userB);

        _sut.Enqueue(BuildMessage("userA", "Alice", username: "alice"));
        _sut.Enqueue(BuildMessage("userB", "Bob", username: "bob"));

        await InvokeFlushAsync();

        await _userRepo.Received(1).UpdateAsync(userA, Arg.Any<CancellationToken>());
        await _userRepo.Received(1).UpdateAsync(userB, Arg.Any<CancellationToken>());
        userA.MessageCount.Should().Be(1);
        userB.MessageCount.Should().Be(1);
    }

    /// <summary>An empty queue must not touch the repository.</summary>
    [Fact]
    public async Task Flush_EmptyQueue_DoesNotTouchRepository()
    {
        await InvokeFlushAsync();

        await _userRepo.DidNotReceiveWithAnyArgs().GetOrCreateAsync(default!, default!, default);
        await _userRepo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    // ─── ISessionStatsCollector Tests ─────────────────────────

    /// <summary>Enqueue accumulates unique chatters and message count for session stats.</summary>
    [Fact]
    public void GetAndResetStats_AfterEnqueue_ReturnsCorrectCounts()
    {
        _sut.Enqueue(BuildMessage("user1"));
        _sut.Enqueue(BuildMessage("user2"));
        _sut.Enqueue(BuildMessage("user1")); // duplicate user

        SessionStats stats = _sut.GetAndResetStats();

        stats.UniqueChatters.Should().Be(2, "user1 and user2 are unique");
        stats.TotalMessages.Should().Be(3, "three messages were enqueued");
        stats.NewFollowers.Should().Be(0);
        stats.NewSubscribers.Should().Be(0);
    }

    /// <summary>GetAndResetStats clears all counters so the next call returns zeros.</summary>
    [Fact]
    public void GetAndResetStats_CalledTwice_SecondCallReturnsZeros()
    {
        _sut.Enqueue(BuildMessage("user1"));
        _sut.RecordFollow();
        _sut.RecordSubscription(3);

        SessionStats first = _sut.GetAndResetStats();
        SessionStats second = _sut.GetAndResetStats();

        first.UniqueChatters.Should().Be(1);
        first.TotalMessages.Should().Be(1);
        first.NewFollowers.Should().Be(1);
        first.NewSubscribers.Should().Be(3);

        second.UniqueChatters.Should().Be(0);
        second.TotalMessages.Should().Be(0);
        second.NewFollowers.Should().Be(0);
        second.NewSubscribers.Should().Be(0);
    }

    /// <summary>RecordFollow increments the follower counter.</summary>
    [Fact]
    public void RecordFollow_IncrementsCounter()
    {
        _sut.RecordFollow();
        _sut.RecordFollow();
        _sut.RecordFollow();

        SessionStats stats = _sut.GetAndResetStats();
        stats.NewFollowers.Should().Be(3);
    }

    /// <summary>RecordSubscription with count accumulates correctly.</summary>
    [Fact]
    public void RecordSubscription_WithCount_Accumulates()
    {
        _sut.RecordSubscription();       // 1
        _sut.RecordSubscription(5);      // gift sub x5
        _sut.RecordSubscription();       // 1

        SessionStats stats = _sut.GetAndResetStats();
        stats.NewSubscribers.Should().Be(7);
    }

    /// <summary>Session stats are independent from the batch flush — flush does not clear session counters.</summary>
    [Fact]
    public async Task Flush_DoesNotClearSessionStats()
    {
        User user = new() { TwitchId = "user1", Username = "user1", MessageCount = 0 };
        _userRepo.GetOrCreateAsync("user1", "user1", Arg.Any<CancellationToken>()).Returns(user);

        _sut.Enqueue(BuildMessage("user1"));
        _sut.RecordFollow();

        // Flush the batch (DB write) — this should NOT reset session stats.
        await InvokeFlushAsync();

        SessionStats stats = _sut.GetAndResetStats();
        stats.UniqueChatters.Should().Be(1, "flush must not clear session counters");
        stats.TotalMessages.Should().Be(1);
        stats.NewFollowers.Should().Be(1);
    }

    /// <summary>RecordChatMessage via the ISessionStatsCollector interface works correctly.</summary>
    [Fact]
    public void RecordChatMessage_ViaInterface_TracksUniqueUsers()
    {
        ISessionStatsCollector collector = _sut;
        collector.RecordChatMessage("userA");
        collector.RecordChatMessage("userB");
        collector.RecordChatMessage("userA");

        SessionStats stats = collector.GetAndResetStats();
        stats.UniqueChatters.Should().Be(2);
        stats.TotalMessages.Should().Be(3, "RecordChatMessage via interface counts each call (3 calls)");
    }

    private static ChatMessage BuildMessage(
        string userId,
        string displayName = "User",
        string? username = null,
        bool isMod = false,
        bool isSub = false)
    {
        string login = username ?? userId;
        return new ChatMessage(userId, login, displayName, "msg", isMod, isSub, false, DateTimeOffset.UtcNow)
        {
            Channel = "testchannel"
        };
    }

    /// <summary>
    /// Invokes the private FlushAsync method via reflection to avoid having to start
    /// the BackgroundService loop in tests.
    /// </summary>
    private async Task InvokeFlushAsync()
    {
        System.Reflection.MethodInfo flush = typeof(UserStatsBatcher).GetMethod(
            "FlushAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        Task task = (Task)flush.Invoke(_sut, new object?[] { CancellationToken.None })!;
        await task;
    }
}
