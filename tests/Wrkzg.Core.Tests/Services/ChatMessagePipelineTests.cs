using System;
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

/// <summary>Tests for the ChatMessagePipeline including command processing and resilience.</summary>
public class ChatMessagePipelineTests
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly IUserTrackingService _trackingService;
    private readonly IUserRepository _userRepo;
    private readonly UserStatsBatcher _statsBatcher;
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
        IStreamStatusProvider streamStatus = Substitute.For<IStreamStatusProvider>();

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

        TimedMessageService timedService = new(scopeFactory, streamStatus, chatClient, TimeProvider.System, Substitute.For<ILogger<TimedMessageService>>());

        ChatGameManager gameManager = new(
            System.Array.Empty<IChatGame>(),
            scopeFactory,
            chatClient,
            Substitute.For<ILogger<ChatGameManager>>());

        Wrkzg.Core.Effects.EffectEngine effectEngine = new(
            System.Array.Empty<Wrkzg.Core.Effects.ITriggerType>(),
            System.Array.Empty<Wrkzg.Core.Effects.IConditionType>(),
            System.Array.Empty<Wrkzg.Core.Effects.IEffectType>(),
            scopeFactory,
            Substitute.For<ILogger<Wrkzg.Core.Effects.EffectEngine>>());

        _statsBatcher = new UserStatsBatcher(scopeFactory, Substitute.For<ILogger<UserStatsBatcher>>());

        _sut = new ChatMessagePipeline(
            _commandProcessor,
            _trackingService,
            timedService,
            gameManager,
            effectEngine,
            broadcaster,
            _statsBatcher,
            scopeFactory,
            _logger);
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

    /// <summary>Verifies that processing a message marks the user as active for watch time tracking.</summary>
    [Fact]
    public async Task ProcessAsync_MarksUserActive()
    {
        ChatMessage msg = CreateMessage(userId: "99999");
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.ProcessAsync(msg);

        _trackingService.Received(1).MarkUserActive("99999");
    }

    /// <summary>Verifies that the pipeline delegates command messages to the command processor.</summary>
    [Fact]
    public async Task ProcessAsync_CallsCommandProcessor()
    {
        ChatMessage msg = CreateMessage("!test");
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.ProcessAsync(msg);

        await _commandProcessor.Received(1).HandleMessageAsync(msg, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the pipeline does not throw when the message has no command prefix.</summary>
    [Fact]
    public async Task ProcessAsync_RegularMessage_DoesNotThrow()
    {
        ChatMessage msg = CreateMessage("hello world");
        _commandProcessor.HandleMessageAsync(msg, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.ProcessAsync(msg);
    }
}
