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

/// <summary>Tests for the RaffleService including creation, entry, drawing, redrawing, and cancellation.</summary>
public class RaffleServiceTests
{
    private readonly IRaffleRepository _raffleRepo;
    private readonly IUserRepository _userRepo;
    private readonly ISettingsRepository _settings;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly ITwitchChatClient _chatClient;
    private readonly RaffleService _sut;

    /// <summary>Initializes test dependencies with NSubstitute mocks.</summary>
    public RaffleServiceTests()
    {
        _raffleRepo = Substitute.For<IRaffleRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _settings = Substitute.For<ISettingsRepository>();
        _broadcaster = Substitute.For<IChatEventBroadcaster>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        ILogger<RaffleService> logger = Substitute.For<ILogger<RaffleService>>();

        _settings.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        _sut = new RaffleService(_raffleRepo, _userRepo, _settings, _broadcaster, _chatClient, logger);
    }

    /// <summary>Verifies that creating a raffle with valid parameters succeeds.</summary>
    [Fact]
    public async Task CreateRaffle_Success()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Raffle?)null);
        _raffleRepo.CreateAsync(Arg.Any<Raffle>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                Raffle r = ci.Arg<Raffle>();
                r.Id = 1;
                return r;
            });

        RaffleResult result = await _sut.CreateAsync("Test Raffle", null, null, null, "TestMod");

        result.Success.Should().BeTrue();
        result.Raffle.Should().NotBeNull();
        result.Raffle!.Title.Should().Be("Test Raffle");
        await _raffleRepo.Received(1).CreateAsync(Arg.Any<Raffle>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that creating a raffle fails when one is already active.</summary>
    [Fact]
    public async Task CreateRaffle_AlreadyActive_Fails()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new Raffle { Id = 1, IsOpen = true, Title = "Existing" });

        RaffleResult result = await _sut.CreateAsync("New Raffle", null, null, null, "TestMod");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already open");
    }

    /// <summary>Verifies that creating a raffle with an empty title fails.</summary>
    [Fact]
    public async Task CreateRaffle_EmptyTitle_Fails()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Raffle?)null);

        RaffleResult result = await _sut.CreateAsync("", null, null, null, "TestMod");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("title");
    }

    /// <summary>Verifies that a keyword starting with exclamation mark is rejected.</summary>
    [Fact]
    public async Task CreateRaffle_InvalidKeyword_WithExclamation_Fails()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Raffle?)null);

        RaffleResult result = await _sut.CreateAsync("Test", "!win", null, null, "TestMod");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("!");
    }

    /// <summary>Verifies that a user can successfully enter an active raffle.</summary>
    [Fact]
    public async Task Enter_Success()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);
        _userRepo.GetOrCreateAsync("123", "testuser", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, TwitchId = "123", Username = "testuser" });
        _raffleRepo.HasUserEnteredAsync(1, 10, Arg.Any<CancellationToken>()).Returns(false);
        _raffleRepo.GetEntryCountAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        EntryResult result = await _sut.EnterAsync("123", "testuser");

        result.Success.Should().BeTrue();
        result.EntryCount.Should().Be(1);
        await _raffleRepo.Received(1).AddEntryAsync(Arg.Any<RaffleEntry>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that a user cannot enter the same raffle twice.</summary>
    [Fact]
    public async Task Enter_AlreadyEntered_Fails()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);
        _userRepo.GetOrCreateAsync("123", "testuser", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, TwitchId = "123", Username = "testuser" });
        _raffleRepo.HasUserEnteredAsync(1, 10, Arg.Any<CancellationToken>()).Returns(true);

        EntryResult result = await _sut.EnterAsync("123", "testuser");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already entered");
    }

    /// <summary>Verifies that entering fails when no raffle is active.</summary>
    [Fact]
    public async Task Enter_NoRaffle_Fails()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Raffle?)null);

        EntryResult result = await _sut.EnterAsync("123", "testuser");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("no active raffle");
    }

    /// <summary>Verifies that entering fails when the raffle has reached its maximum entry count.</summary>
    [Fact]
    public async Task Enter_MaxReached_Fails()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            MaxEntries = 5,
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);
        _raffleRepo.GetEntryCountAsync(1, Arg.Any<CancellationToken>()).Returns(5);

        EntryResult result = await _sut.EnterAsync("123", "testuser");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("full");
    }

    /// <summary>Verifies that drawing sets a pending winner without closing the raffle.</summary>
    [Fact]
    public async Task Draw_SetsPendingWinner_DoesNotCloseRaffle()
    {
        User winner = new() { Id = 10, TwitchId = "123", Username = "winner", DisplayName = "Winner" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Draws = new List<RaffleDraw>(),
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = winner }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.DrawAsync();

        result.Success.Should().BeTrue();
        result.WinnerName.Should().Be("Winner");
        raffle.IsOpen.Should().BeTrue();
        raffle.PendingWinnerId.Should().Be(10);
        await _raffleRepo.Received(1).AddDrawAsync(Arg.Any<RaffleDraw>(), Arg.Any<CancellationToken>());
        await _broadcaster.Received(1).BroadcastRaffleDrawPendingAsync(
            1, "Winner", "123", 1, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that drawing fails when a winner is already pending verification.</summary>
    [Fact]
    public async Task Draw_AlreadyPending_Fails()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            PendingWinnerId = 10,
            Draws = new List<RaffleDraw>(),
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10 }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.DrawAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("pending verification");
    }

    /// <summary>Verifies that accepting a winner clears the pending state while keeping the raffle open.</summary>
    [Fact]
    public async Task AcceptWinner_ClearsPending_RaffleStaysOpen()
    {
        User winner = new() { Id = 10, TwitchId = "123", DisplayName = "Winner" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            PendingWinnerId = 10,
            PendingWinner = winner,
            Draws = new List<RaffleDraw>
            {
                new() { RaffleId = 1, UserId = 10, DrawNumber = 1, User = winner }
            },
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = winner }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.AcceptWinnerAsync();

        result.Success.Should().BeTrue();
        result.WinnerName.Should().Be("Winner");
        raffle.IsOpen.Should().BeTrue();
        raffle.PendingWinnerId.Should().BeNull();
        raffle.Draws[0].IsAccepted.Should().BeTrue();
        await _broadcaster.Received(1).BroadcastRaffleWinnerAcceptedAsync(
            1, "Winner", 1, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that ending a raffle closes it and sets the final winner.</summary>
    [Fact]
    public async Task EndRaffle_ClosesRaffle()
    {
        User winner = new() { Id = 10, TwitchId = "123", DisplayName = "Winner" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Draws = new List<RaffleDraw>
            {
                new() { RaffleId = 1, UserId = 10, DrawNumber = 1, IsAccepted = true, User = winner }
            },
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = winner }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        RaffleResult result = await _sut.EndRaffleAsync();

        result.Success.Should().BeTrue();
        raffle.IsOpen.Should().BeFalse();
        raffle.WinnerId.Should().Be(10);
        raffle.EndReason.Should().Be(RaffleEndReason.Drawn);
        await _broadcaster.Received(1).BroadcastRaffleEndedAsync(1, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that ending a raffle fails when none is active.</summary>
    [Fact]
    public async Task EndRaffle_NoActiveRaffle_Fails()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Raffle?)null);

        RaffleResult result = await _sut.EndRaffleAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No active raffle");
    }

    /// <summary>Verifies that ending a raffle with a pending winner rejects the pending draw.</summary>
    [Fact]
    public async Task EndRaffle_WithPendingWinner_RejectsPending()
    {
        User user1 = new() { Id = 10, TwitchId = "100", DisplayName = "User1" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            PendingWinnerId = 10,
            Draws = new List<RaffleDraw>
            {
                new() { RaffleId = 1, UserId = 10, DrawNumber = 1, User = user1 }
            },
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = user1 }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        RaffleResult result = await _sut.EndRaffleAsync();

        result.Success.Should().BeTrue();
        raffle.IsOpen.Should().BeFalse();
        raffle.PendingWinnerId.Should().BeNull();
        raffle.Draws[0].RedrawReason.Should().Be("Raffle ended");
    }

    /// <summary>Verifies that accepting a winner fails when no winner is pending.</summary>
    [Fact]
    public async Task AcceptWinner_NoPending_Fails()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            PendingWinnerId = null,
            Draws = new List<RaffleDraw>(),
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.AcceptWinnerAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No pending winner");
    }

    /// <summary>Verifies that drawing fails when the raffle has no entries.</summary>
    [Fact]
    public async Task Draw_NoEntries_Fails()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Draws = new List<RaffleDraw>(),
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.DrawAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No entries");
    }

    /// <summary>Verifies that drawing fails when no raffle is active.</summary>
    [Fact]
    public async Task Draw_NoRaffle_Fails()
    {
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((Raffle?)null);

        DrawResult result = await _sut.DrawAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("No active raffle");
    }

    /// <summary>Verifies that cancelling a raffle closes it with the Cancelled end reason.</summary>
    [Fact]
    public async Task Cancel_Success()
    {
        Raffle raffle = new() { Id = 1, IsOpen = true, Title = "Test", Entries = new List<RaffleEntry>() };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        RaffleResult result = await _sut.CancelAsync();

        result.Success.Should().BeTrue();
        raffle.IsOpen.Should().BeFalse();
        raffle.EndReason.Should().Be(RaffleEndReason.Cancelled);
    }

    /// <summary>Verifies that a matching keyword message enters the user into the raffle.</summary>
    [Fact]
    public async Task TryKeywordEntry_MatchingKeyword_EntersUser()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Keyword = "win",
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);
        _userRepo.GetOrCreateAsync("123", "testuser", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, TwitchId = "123", Username = "testuser" });
        _raffleRepo.HasUserEnteredAsync(1, 10, Arg.Any<CancellationToken>()).Returns(false);
        _raffleRepo.GetEntryCountAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        bool result = await _sut.TryKeywordEntryAsync("123", "testuser", "win");

        result.Should().BeTrue();
        await _raffleRepo.Received(1).AddEntryAsync(Arg.Any<RaffleEntry>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that a non-matching message does not enter the user.</summary>
    [Fact]
    public async Task TryKeywordEntry_NonMatchingMessage_ReturnsFalse()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Keyword = "win",
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        bool result = await _sut.TryKeywordEntryAsync("123", "testuser", "hello everyone");

        result.Should().BeFalse();
    }

    /// <summary>Verifies that a redraw excludes previously drawn users and selects a new winner.</summary>
    [Fact]
    public async Task Redraw_ExcludesPreviouslyDrawnUsers()
    {
        User user1 = new() { Id = 10, TwitchId = "100", DisplayName = "User1" };
        User user2 = new() { Id = 20, TwitchId = "200", DisplayName = "User2" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            PendingWinnerId = 10,
            Draws = new List<RaffleDraw>
            {
                new() { RaffleId = 1, UserId = 10, DrawNumber = 1 }
            },
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = user1 },
                new() { Id = 2, RaffleId = 1, UserId = 20, User = user2 }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.RedrawAsync("Not present");

        result.Success.Should().BeTrue();
        result.WinnerName.Should().Be("User2");
        raffle.PendingWinnerId.Should().Be(20);
    }

    /// <summary>Verifies that drawing fails when all entries have already been drawn.</summary>
    [Fact]
    public async Task Draw_AllDrawn_Fails()
    {
        User user1 = new() { Id = 10, TwitchId = "100", DisplayName = "User1" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            Draws = new List<RaffleDraw>
            {
                new() { RaffleId = 1, UserId = 10, DrawNumber = 1 }
            },
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = user1 }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        DrawResult result = await _sut.DrawAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("All entries have been drawn");
    }

    /// <summary>Verifies that an expired raffle with entries auto-draws a pending winner.</summary>
    [Fact]
    public async Task CheckExpired_AutoDraws_SetsPending()
    {
        User user1 = new() { Id = 10, TwitchId = "100", DisplayName = "User1" };
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            EntriesCloseAt = DateTimeOffset.UtcNow.AddSeconds(-10),
            Draws = new List<RaffleDraw>(),
            Entries = new List<RaffleEntry>
            {
                new() { Id = 1, RaffleId = 1, UserId = 10, User = user1 }
            }
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        await _sut.CheckExpiredRafflesAsync();

        raffle.PendingWinnerId.Should().Be(10);
        raffle.IsOpen.Should().BeTrue();
    }

    /// <summary>Verifies that an expired raffle with no entries is automatically cancelled.</summary>
    [Fact]
    public async Task CheckExpired_NoEntries_Cancels()
    {
        Raffle raffle = new()
        {
            Id = 1,
            IsOpen = true,
            Title = "Test",
            EntriesCloseAt = DateTimeOffset.UtcNow.AddSeconds(-10),
            Draws = new List<RaffleDraw>(),
            Entries = new List<RaffleEntry>()
        };
        _raffleRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(raffle);

        await _sut.CheckExpiredRafflesAsync();

        raffle.IsOpen.Should().BeFalse();
        raffle.EndReason.Should().Be(RaffleEndReason.Cancelled);
    }
}
