using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;
using Xunit;

namespace Wrkzg.Core.Tests.Services;

/// <summary>Tests for the ChatMessagePipeline including user stats, command processing, and resilience.</summary>
public class ChatMessagePipelineTests
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IUserTrackingService _trackingService;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<ChatMessagePipeline> _logger;
    private readonly ChatMessagePipeline _sut;

    /// <summary>Initializes test dependencies with NSubstitute mocks and a real service scope factory.</summary>
    public ChatMessagePipelineTests()
    {
        _commandProcessor = Substitute.For<ICommandProcessor>();
        _trackingService = Substitute.For<IUserTrackingService>();
        _userRepo = Substitute.For<IUserRepository>();
        _logger = Substitute.For<ILogger<ChatMessagePipeline>>();

        IRaffleRepository raffleRepo = Substitute.For<IRaffleRepository>();
        ISettingsRepository settingsRepo = Substitute.For<ISettingsRepository>();
        IChatEventBroadcaster broadcaster = Substitute.For<IChatEventBroadcaster>();
        ITwitchChatClient chatClient = Substitute.For<ITwitchChatClient>();

        ServiceCollection services = new();
        services.AddScoped(_ => _userRepo);
        services.AddScoped(_ => raffleRepo);
        services.AddScoped(_ => settingsRepo);
        services.AddSingleton(broadcaster);
        services.AddSingleton(chatClient);
        services.AddSingleton(Substitute.For<ILogger<RaffleService>>());
        services.AddScoped<RaffleService>();
        services.AddScoped(_ => Substitute.For<IBroadcasterHelixClient>());
        services.AddScoped(_ => Substitute.For<IBotHelixClient>());
        services.AddScoped(_ => Substitute.For<ISecureStorage>());
        services.AddScoped(_ => Substitute.For<ITwitchOAuthService>());
        services.AddSingleton(Substitute.For<ILogger<SpamFilterService>>());
        services.AddScoped<SpamFilterService>();
        services.AddScoped(_ => Substitute.For<ICounterRepository>());
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        TimedMessageService timedService = new(scopeFactory, chatClient, Substitute.For<ILogger<TimedMessageService>>());

        // ChatGameManager with no games registered (empty list)
        ChatGameManager gameManager = new(
            System.Array.Empty<IChatGame>(),
            scopeFactory,
            chatClient,
            Substitute.For<ILogger<ChatGameManager>>());

        // EffectEngine with no types registered (empty lists)
        Wrkzg.Core.Effects.EffectEngine effectEngine = new(
            System.Array.Empty<Wrkzg.Core.Effects.ITriggerType>(),
            System.Array.Empty<Wrkzg.Core.Effects.IConditionType>(),
            System.Array.Empty<Wrkzg.Core.Effects.IEffectType>(),
            scopeFactory,
            Substitute.For<ILogger<Wrkzg.Core.Effects.EffectEngine>>());

        _sut = new ChatMessagePipeline(_commandProcessor, _trackingService, timedService, gameManager, effectEngine, broadcaster, scopeFactory, _logger);
    }

    private static ChatMessage CreateMessage(
        string content = "hello",
        string userId = "12345",
        string username = "testuser",
        string displayName = "TestUser",
        bool isMod = false,
        bool isSub = false)
    {
        return new ChatMessage(userId, username, displayName, content, isMod, isSub, false, DateTimeOffset.UtcNow)
        {
            Channel = "testchannel"
        };
    }

    /// <summary>Verifies that processing a message increments the user's message count and updates the display name.</summary>
    [Fact]
    public async Task ProcessAsync_UpdatesUserStats()
    {
        ChatMessage msg = CreateMessage();
        User user = new() { TwitchId = "12345", Username = "testuser", DisplayName = "TestUser", MessageCount = 5 };
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.ProcessAsync(msg);

        user.MessageCount.Should().Be(6);
        user.DisplayName.Should().Be("TestUser");
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that processing a message marks the user as active for watch time tracking.</summary>
    [Fact]
    public async Task ProcessAsync_MarksUserActive()
    {
        ChatMessage msg = CreateMessage(userId: "99999");
        User user = new() { TwitchId = "99999", Username = "testuser" };
        _userRepo.GetOrCreateAsync("99999", "testuser", Arg.Any<CancellationToken>()).Returns(user);
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.ProcessAsync(msg);

        _trackingService.Received(1).MarkUserActive("99999");
    }

    /// <summary>Verifies that the pipeline delegates command messages to the command processor.</summary>
    [Fact]
    public async Task ProcessAsync_CallsCommandProcessor()
    {
        ChatMessage msg = CreateMessage("!test");
        User user = new() { TwitchId = "12345", Username = "testuser" };
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.ProcessAsync(msg);

        await _commandProcessor.Received(1).HandleMessageAsync(msg, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the pipeline syncs moderator status from the chat message to the user record.</summary>
    [Fact]
    public async Task ProcessAsync_SyncsModStatus()
    {
        ChatMessage msg = CreateMessage(isMod: true);
        User user = new() { TwitchId = "12345", Username = "testuser", IsMod = false };
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.ProcessAsync(msg);

        user.IsMod.Should().BeTrue();
    }

    /// <summary>Verifies that the pipeline syncs subscriber status from the chat message to the user record.</summary>
    [Fact]
    public async Task ProcessAsync_SyncsSubscriberStatus()
    {
        ChatMessage msg = CreateMessage(isSub: true);
        User user = new() { TwitchId = "12345", Username = "testuser", IsSubscriber = false };
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.ProcessAsync(msg);

        user.IsSubscriber.Should().BeTrue();
    }

    /// <summary>Verifies that a database failure during stats update does not prevent command processing.</summary>
    [Fact]
    public async Task ProcessAsync_StatsFailure_DoesNotBreakCommandProcessing()
    {
        ChatMessage msg = CreateMessage("!test");
        _userRepo.GetOrCreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB connection failed"));
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(true);

        // Should not throw — stats failure is caught internally
        await _sut.ProcessAsync(msg);

        // Command processing should still have been attempted
        await _commandProcessor.Received(1).HandleMessageAsync(msg, Arg.Any<CancellationToken>());
    }
}
