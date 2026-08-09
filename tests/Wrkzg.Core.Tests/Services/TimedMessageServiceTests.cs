using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;
using Xunit;

namespace Wrkzg.Core.Tests.Services;

/// <summary>Tests for the TimedMessageService background service scheduling logic.</summary>
public class TimedMessageServiceTests
{
    private readonly ITimedMessageRepository _timerRepo;
    private readonly ITwitchChatClient _chatClient;
    private readonly IBotHelixClient _botHelix;
    private readonly IStreamStatusProvider _streamStatus;
    private readonly TestTimeProvider _time;
    private readonly TimedMessageService _sut;

    /// <summary>Initializes the test fixture.</summary>
    public TimedMessageServiceTests()
    {
        _timerRepo = Substitute.For<ITimedMessageRepository>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        _botHelix = Substitute.For<IBotHelixClient>();
        _streamStatus = Substitute.For<IStreamStatusProvider>();
        _chatClient.IsConnected.Returns(true);

        ServiceCollection services = new();
        services.AddScoped(_ => _timerRepo);
        services.AddScoped(_ => _botHelix);
        services.AddScoped(_ => Substitute.For<ISecureStorage>());
        services.AddScoped(_ => Substitute.For<ITwitchOAuthService>());
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _time = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _sut = new TimedMessageService(
            scopeFactory,
            _streamStatus,
            _chatClient,
            _time,
            Substitute.For<ILogger<TimedMessageService>>());
    }

    /// <summary>
    /// Regression: when the stream goes live, timers that are overdue because of the offline gap
    /// (their LastFiredAt predates this stream) must NOT all fire in the same tick. Before the fix
    /// every timer became overdue at go-live and fired simultaneously.
    /// </summary>
    [Fact]
    public async Task GoingLive_DoesNotFireOverdueTimers_AtOnce()
    {
        DateTimeOffset previousStream = _time.GetUtcNow().AddDays(-1);
        List<TimedMessage> timers = new()
        {
            OnlineTimer(1, intervalMinutes: 10, lastFired: previousStream),
            OnlineTimer(2, intervalMinutes: 20, lastFired: previousStream),
            OnlineTimer(3, intervalMinutes: 30, lastFired: previousStream)
        };
        _timerRepo.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns(timers);
        _streamStatus.IsLive.Returns(true); // _wasLive defaults to false → this tick is the offline→online edge

        await _sut.CheckAndFireTimersAsync(CancellationToken.None);

        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _timerRepo.DidNotReceive().UpdateAsync(Arg.Any<TimedMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// After going live a timer's first fire happens exactly one interval later — not immediately,
    /// and not before the interval has elapsed.
    /// </summary>
    [Fact]
    public async Task AfterGoingLive_TimerFires_OneIntervalLater()
    {
        TimedMessage timer = OnlineTimer(1, intervalMinutes: 10, lastFired: _time.GetUtcNow().AddDays(-1));
        _timerRepo.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns(new List<TimedMessage> { timer });
        _streamStatus.IsLive.Returns(true);

        // Go-live tick: nothing fires.
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Nine minutes in: still below the interval.
        _time.Advance(TimeSpan.FromMinutes(9));
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Ten minutes in: fires exactly once.
        _time.Advance(TimeSpan.FromMinutes(1));
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _chatClient.Received(1).SendMessageAsync("msg1", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// When the bot starts up while the stream is already live, overdue timers must not burst on
    /// the first tick either (the startup analog of the go-live burst).
    /// </summary>
    [Fact]
    public async Task StartingUpWhileAlreadyLive_DoesNotBurst()
    {
        DateTimeOffset previousStream = _time.GetUtcNow().AddDays(-1);
        List<TimedMessage> timers = new()
        {
            OnlineTimer(1, intervalMinutes: 10, lastFired: previousStream),
            OnlineTimer(2, intervalMinutes: 15, lastFired: previousStream)
        };
        _timerRepo.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns(timers);
        _streamStatus.IsLive.Returns(true);

        await _sut.CheckAndFireTimersAsync(CancellationToken.None);

        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Regression: MinChatLines gates on chat lines accumulated SINCE the timer last fired, not on
    /// lines seen within a single 30-second poll window (which was reset every tick before the fix).
    /// </summary>
    [Fact]
    public async Task MinChatLines_GatesOnLinesSinceLastFire_NotPerPollWindow()
    {
        TimedMessage timer = new()
        {
            Id = 1,
            Name = "T",
            Messages = new[] { "hi" },
            IsEnabled = true,
            RunWhenOnline = true,
            RunWhenOffline = true,
            IntervalMinutes = 1,
            MinChatLines = 5,
            LastFiredAt = _time.GetUtcNow().AddHours(-1) // interval already satisfied
        };
        _timerRepo.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns(new List<TimedMessage> { timer });
        _streamStatus.IsLive.Returns(false); // offline path isolates the MinChatLines gate from the live anchor

        // Only 3 lines so far (< 5) → does not fire, even across multiple ticks.
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Two more lines (5 total since start) → fires once.
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
        _time.Advance(TimeSpan.FromMinutes(2));
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _chatClient.Received(1).SendMessageAsync("hi", Arg.Any<CancellationToken>());

        // Baseline reset on fire: two more lines (7 total, only 2 since last fire) → does not fire again.
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
        _time.Advance(TimeSpan.FromMinutes(2));
        await _sut.CheckAndFireTimersAsync(CancellationToken.None);
        await _chatClient.Received(1).SendMessageAsync("hi", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A never-fired offline timer with no chat-line requirement still fires immediately — this
    /// behavior is intentionally unchanged; the anchor only applies while the stream is live.
    /// </summary>
    [Fact]
    public async Task OfflineFreshTimer_FiresImmediately()
    {
        TimedMessage timer = new()
        {
            Id = 1,
            Name = "T",
            Messages = new[] { "hi" },
            IsEnabled = true,
            RunWhenOnline = true,
            RunWhenOffline = true,
            IntervalMinutes = 10,
            MinChatLines = 0,
            LastFiredAt = null
        };
        _timerRepo.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns(new List<TimedMessage> { timer });
        _streamStatus.IsLive.Returns(false);

        await _sut.CheckAndFireTimersAsync(CancellationToken.None);

        await _chatClient.Received(1).SendMessageAsync("hi", Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the chat line counter increments atomically without errors.</summary>
    [Fact]
    public void IncrementChatLineCounter_IncrementsAtomically()
    {
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
    }

    private static TimedMessage OnlineTimer(int id, int intervalMinutes, DateTimeOffset? lastFired) => new()
    {
        Id = id,
        Name = $"Timer{id}",
        Messages = new[] { $"msg{id}" },
        IsEnabled = true,
        RunWhenOnline = true,
        RunWhenOffline = false,
        MinChatLines = 0,
        IntervalMinutes = intervalMinutes,
        LastFiredAt = lastFired
    };

    /// <summary>Deterministic <see cref="TimeProvider"/> for tests — advances only when told to.</summary>
    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public TestTimeProvider(DateTimeOffset start)
        {
            _now = start;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
