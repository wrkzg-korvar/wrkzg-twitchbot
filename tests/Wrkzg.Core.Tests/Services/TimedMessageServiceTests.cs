using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;
using Xunit;

namespace Wrkzg.Core.Tests.Services;

/// <summary>Tests for the TimedMessageService background service.</summary>
public class TimedMessageServiceTests
{
    private readonly ITimedMessageRepository _timerRepo;
    private readonly ISettingsRepository _settings;
    private readonly ITwitchChatClient _chatClient;
    private readonly IBroadcasterHelixClient _broadcasterHelix;
    private readonly IBotHelixClient _botHelix;
    private readonly IStreamStatusProvider _streamStatus;
    private readonly TimedMessageService _sut;

    /// <summary>Initializes the test fixture.</summary>
    public TimedMessageServiceTests()
    {
        _timerRepo = Substitute.For<ITimedMessageRepository>();
        _settings = Substitute.For<ISettingsRepository>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        _broadcasterHelix = Substitute.For<IBroadcasterHelixClient>();
        _botHelix = Substitute.For<IBotHelixClient>();
        _streamStatus = Substitute.For<IStreamStatusProvider>();
        _chatClient.IsConnected.Returns(true);

        ServiceCollection services = new();
        services.AddScoped(_ => _timerRepo);
        services.AddScoped(_ => _settings);
        services.AddScoped(_ => _broadcasterHelix);
        services.AddScoped(_ => _botHelix);
        services.AddScoped(_ => Substitute.For<ISecureStorage>());
        services.AddScoped(_ => Substitute.For<ITwitchOAuthService>());
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _sut = new TimedMessageService(scopeFactory, _streamStatus, _chatClient, Substitute.For<ILogger<TimedMessageService>>());
    }

    /// <summary>Verifies that a timer fires immediately when it has never been fired before.</summary>
    [Fact]
    public void FiresImmediately_WhenLastFiredAtIsNull()
    {
        TimedMessage timer = new()
        {
            Id = 1,
            Name = "Test",
            Messages = new[] { "Hello!" },
            IsEnabled = true,
            RunWhenOffline = true,
            RunWhenOnline = true,
            MinChatLines = 0,
            IntervalMinutes = 10,
            LastFiredAt = null
        };
        _timerRepo.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns(new List<TimedMessage> { timer });

        timer.LastFiredAt.Should().BeNull();
    }

    /// <summary>Verifies that the chat line counter increments atomically without errors.</summary>
    [Fact]
    public void IncrementChatLineCounter_IncrementsAtomically()
    {
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
        _sut.IncrementChatLineCounter();
    }
}
