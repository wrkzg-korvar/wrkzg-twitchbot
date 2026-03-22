using System;
using System.Collections.Generic;
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

public class PollServiceTests
{
    private readonly IPollRepository _pollRepo;
    private readonly IUserRepository _userRepo;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ITwitchChatClient _chatClient;
    private readonly ISettingsRepository _settings;
    private readonly PollService _sut;

    public PollServiceTests()
    {
        _pollRepo = Substitute.For<IPollRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _broadcaster = Substitute.For<IChatEventBroadcaster>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        _settings = Substitute.For<ISettingsRepository>();
        ILogger<PollService> logger = Substitute.For<ILogger<PollService>>();

        // Settings returns null by default (= use defaults)
        _settings.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        _sut = new PollService(_pollRepo, _userRepo, _broadcaster, _chatClient, _settings, logger);
    }

    [Fact]
    public async Task CreateBotPoll_Success()
    {
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Poll?)null);
        _pollRepo.CreateAsync(Arg.Any<Poll>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                Poll p = ci.Arg<Poll>();
                p.Id = 1;
                return p;
            });

        PollResult result = await _sut.CreateBotPollAsync("Test?", new[] { "A", "B" }, 60, "TestUser");

        result.Success.Should().BeTrue();
        result.Poll.Should().NotBeNull();
        result.Poll!.Question.Should().Be("Test?");
        result.Poll.Options.Should().BeEquivalentTo(new[] { "A", "B" });
        result.Poll.DurationSeconds.Should().Be(60);
        await _pollRepo.Received(1).CreateAsync(Arg.Any<Poll>(), Arg.Any<CancellationToken>());
        await _broadcaster.Received(1).BroadcastPollCreatedAsync(Arg.Any<Poll>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBotPoll_ActivePollExists_Fails()
    {
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Poll { Id = 1, IsActive = true, Question = "Existing?" });

        PollResult result = await _sut.CreateBotPollAsync("New?", new[] { "A", "B" }, 60, "TestUser");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already active");
    }

    [Fact]
    public async Task CreateBotPoll_TooFewOptions_Fails()
    {
        PollResult result = await _sut.CreateBotPollAsync("Test?", new[] { "A" }, 60, "TestUser");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("2-5 options");
    }

    [Fact]
    public async Task CreateBotPoll_TooManyOptions_Fails()
    {
        PollResult result = await _sut.CreateBotPollAsync("Test?", new[] { "A", "B", "C", "D", "E", "F" }, 60, "TestUser");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("2-5 options");
    }

    [Fact]
    public async Task Vote_Success()
    {
        Poll poll = new()
        {
            Id = 1,
            IsActive = true,
            Options = new[] { "A", "B" },
            EndsAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Votes = new List<PollVote>()
        };
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(poll);
        _userRepo.GetOrCreateAsync("123", "testuser", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, TwitchId = "123", Username = "testuser" });
        _pollRepo.HasUserVotedAsync(1, 10, Arg.Any<CancellationToken>()).Returns(false);

        VoteResult result = await _sut.VoteAsync("123", "testuser", 0);

        result.Success.Should().BeTrue();
        result.OptionText.Should().Be("A");
        await _pollRepo.Received(1).AddVoteAsync(Arg.Any<PollVote>(), Arg.Any<CancellationToken>());
        await _broadcaster.Received(1).BroadcastPollVoteAsync(1, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Vote_AlreadyVoted_Fails()
    {
        Poll poll = new()
        {
            Id = 1,
            IsActive = true,
            Options = new[] { "A", "B" },
            EndsAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Votes = new List<PollVote>()
        };
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(poll);
        _userRepo.GetOrCreateAsync("123", "testuser", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, TwitchId = "123", Username = "testuser" });
        _pollRepo.HasUserVotedAsync(1, 10, Arg.Any<CancellationToken>()).Returns(true);

        VoteResult result = await _sut.VoteAsync("123", "testuser", 0);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already voted");
    }

    [Fact]
    public async Task Vote_NoPoll_Fails()
    {
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Poll?)null);

        VoteResult result = await _sut.VoteAsync("123", "testuser", 0);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("no active poll");
    }

    [Fact]
    public async Task Vote_InvalidOption_Fails()
    {
        Poll poll = new()
        {
            Id = 1,
            IsActive = true,
            Options = new[] { "A", "B" },
            EndsAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Votes = new List<PollVote>()
        };
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(poll);

        VoteResult result = await _sut.VoteAsync("123", "testuser", 5);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("invalid option");
    }

    [Fact]
    public async Task EndPoll_Success()
    {
        Poll poll = new()
        {
            Id = 1,
            IsActive = true,
            Question = "Test?",
            Options = new[] { "A", "B" },
            Votes = new List<PollVote>
            {
                new() { PollId = 1, UserId = 1, OptionIndex = 0 },
                new() { PollId = 1, UserId = 2, OptionIndex = 0 },
                new() { PollId = 1, UserId = 3, OptionIndex = 1 }
            }
        };
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(poll);

        PollResult result = await _sut.EndPollAsync(PollEndReason.ManuallyClosed);

        result.Success.Should().BeTrue();
        poll.IsActive.Should().BeFalse();
        poll.EndReason.Should().Be(PollEndReason.ManuallyClosed);
        await _pollRepo.Received(1).UpdateAsync(poll, Arg.Any<CancellationToken>());
        await _broadcaster.Received(1).BroadcastPollEndedAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndPoll_NoPoll_Fails()
    {
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Poll?)null);

        PollResult result = await _sut.EndPollAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No active poll");
    }

    [Fact]
    public async Task CheckExpired_ClosesExpiredPoll()
    {
        Poll poll = new()
        {
            Id = 1,
            IsActive = true,
            Question = "Test?",
            Options = new[] { "A", "B" },
            EndsAt = DateTimeOffset.UtcNow.AddSeconds(-10),
            Votes = new List<PollVote>()
        };
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(poll);

        await _sut.CheckExpiredPollsAsync();

        poll.IsActive.Should().BeFalse();
        poll.EndReason.Should().Be(PollEndReason.TimerExpired);
        await _pollRepo.Received(1).UpdateAsync(poll, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Vote_CustomTemplate_UsesOverride()
    {
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Poll?)null);
        _settings.GetAsync("poll.vote.no_poll", Arg.Any<CancellationToken>())
            .Returns("Hey {user}, there is nothing to vote on!");

        VoteResult result = await _sut.VoteAsync("123", "testuser", 0);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Hey testuser, there is nothing to vote on!");
    }

    [Fact]
    public async Task Create_CustomStartTemplate_UsesOverride()
    {
        _pollRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Poll?)null);
        _pollRepo.CreateAsync(Arg.Any<Poll>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                Poll p = ci.Arg<Poll>();
                p.Id = 1;
                return p;
            });
        _settings.GetAsync("poll.announce.start", Arg.Any<CancellationToken>())
            .Returns("NEW POLL: {question} -- {options}");
        _chatClient.IsConnected.Returns(true);

        PollResult result = await _sut.CreateBotPollAsync("Best game?", new[] { "A", "B" }, 60, "Mod1");

        result.Success.Should().BeTrue();
        await _chatClient.Received(1).SendMessageAsync(
            "NEW POLL: Best game? -- [1] A [2] B", Arg.Any<CancellationToken>());
    }
}
