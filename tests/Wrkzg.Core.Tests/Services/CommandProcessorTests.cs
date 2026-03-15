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

public class CommandProcessorTests
{
    private readonly ICommandRepository _commandRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITwitchChatClient _chatClient;
    private readonly ILogger<CommandProcessor> _logger;
    private readonly CommandProcessor _sut;

    public CommandProcessorTests()
    {
        _commandRepo = Substitute.For<ICommandRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        _logger = Substitute.For<ILogger<CommandProcessor>>();

        // Build a real IServiceScopeFactory that returns our mocks
        ServiceCollection services = new();
        services.AddScoped(_ => _commandRepo);
        services.AddScoped(_ => _userRepo);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _sut = new CommandProcessor(scopeFactory, _chatClient, Array.Empty<ISystemCommand>(), _logger);
    }

    private static ChatMessage CreateMessage(
        string content,
        string userId = "12345",
        string username = "testuser",
        string displayName = "TestUser",
        bool isMod = false,
        bool isSub = false,
        bool isBroadcaster = false)
    {
        return new ChatMessage(
            UserId: userId,
            Username: username,
            DisplayName: displayName,
            Content: content,
            IsModerator: isMod,
            IsSubscriber: isSub,
            IsBroadcaster: isBroadcaster,
            Timestamp: DateTimeOffset.UtcNow)
        {
            Channel = "testchannel"
        };
    }

    private static User CreateUser(
        string twitchId = "12345",
        long points = 100,
        int messageCount = 50,
        int watchedMinutes = 120,
        bool isMod = false,
        bool isSub = false,
        DateTimeOffset? followDate = null)
    {
        return new User
        {
            Id = 1,
            TwitchId = twitchId,
            Username = "testuser",
            DisplayName = "TestUser",
            Points = points,
            MessageCount = messageCount,
            WatchedMinutes = watchedMinutes,
            IsMod = isMod,
            IsSubscriber = isSub,
            FollowDate = followDate
        };
    }

    private static Command CreateCommand(
        string trigger = "!test",
        string response = "Hello {user}!",
        PermissionLevel permission = PermissionLevel.Everyone,
        int globalCooldown = 0,
        int userCooldown = 0,
        bool isEnabled = true)
    {
        return new Command
        {
            Id = 1,
            Trigger = trigger,
            Aliases = Array.Empty<string>(),
            ResponseTemplate = response,
            PermissionLevel = permission,
            GlobalCooldownSeconds = globalCooldown,
            UserCooldownSeconds = userCooldown,
            IsEnabled = isEnabled,
            UseCount = 0
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    // Basic Command Execution
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleMessageAsync_ValidCommand_SendsResponseAndReturnsTrue()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand();
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
        await _chatClient.Received(1).SendMessageAsync("Hello TestUser!", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ValidCommand_IncrementsUseCount()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand();
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        cmd.UseCount.Should().Be(1);
        await _commandRepo.Received(1).UpdateAsync(cmd, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_MessageWithoutPrefix_ReturnsFalse()
    {
        ChatMessage msg = CreateMessage("hello world");

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeFalse();
        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_EmptyMessage_ReturnsFalse()
    {
        ChatMessage msg = CreateMessage("");

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_UnknownCommand_ReturnsFalse()
    {
        ChatMessage msg = CreateMessage("!unknown");

        _commandRepo.GetByTriggerOrAliasAsync("!unknown", Arg.Any<CancellationToken>())
            .Returns((Command?)null);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeFalse();
        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_DisabledCommand_ReturnsFalse()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(isEnabled: false);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_TriggerIsCaseInsensitive()
    {
        ChatMessage msg = CreateMessage("!TEST");
        Command cmd = CreateCommand(trigger: "!test");
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_MessageWithArguments_ExtractsOnlyTrigger()
    {
        ChatMessage msg = CreateMessage("!test some extra args");
        Command cmd = CreateCommand();
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Permission Checks
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleMessageAsync_BroadcasterCanUseAnyCommand()
    {
        ChatMessage msg = CreateMessage("!test", isBroadcaster: true);
        Command cmd = CreateCommand(permission: PermissionLevel.Broadcaster);
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_ModCanUseModCommand()
    {
        ChatMessage msg = CreateMessage("!test", isMod: true);
        Command cmd = CreateCommand(permission: PermissionLevel.Moderator);
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_ViewerCannotUseModCommand()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(permission: PermissionLevel.Moderator);
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeFalse();
        await _chatClient.DidNotReceive().SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_SubscriberCanUseSubCommand()
    {
        ChatMessage msg = CreateMessage("!test", isSub: true);
        Command cmd = CreateCommand(permission: PermissionLevel.Subscriber);
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_FollowerCanUseFollowerCommand()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(permission: PermissionLevel.Follower);
        User user = CreateUser(followDate: DateTimeOffset.UtcNow.AddDays(-30));

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_NonFollowerCannotUseFollowerCommand()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(permission: PermissionLevel.Follower);
        User user = CreateUser(followDate: null);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_UserIsMod_FromDbFlag_CanUseModCommand()
    {
        // User is mod according to DB (user.IsMod) but NOT according to the chat message
        ChatMessage msg = CreateMessage("!test", isMod: false);
        Command cmd = CreateCommand(permission: PermissionLevel.Moderator);
        User user = CreateUser(isMod: true);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool result = await _sut.HandleMessageAsync(msg);

        result.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Cooldowns
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleMessageAsync_GlobalCooldown_BlocksSecondCall()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(globalCooldown: 30);
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        // First call — should succeed
        bool first = await _sut.HandleMessageAsync(msg);
        first.Should().BeTrue();

        // Second call immediately — should be blocked by cooldown
        bool second = await _sut.HandleMessageAsync(msg);
        second.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_UserCooldown_BlocksSameUser()
    {
        Command cmd = CreateCommand(userCooldown: 30);
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync("12345", "testuser", Arg.Any<CancellationToken>()).Returns(user);

        ChatMessage msg1 = CreateMessage("!test", userId: "12345");

        bool first = await _sut.HandleMessageAsync(msg1);
        first.Should().BeTrue();

        bool second = await _sut.HandleMessageAsync(msg1);
        second.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_UserCooldown_AllowsDifferentUser()
    {
        Command cmd = CreateCommand(userCooldown: 30);
        User user1 = CreateUser(twitchId: "111");
        User user2 = CreateUser(twitchId: "222");

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync("111", "user1", Arg.Any<CancellationToken>()).Returns(user1);
        _userRepo.GetOrCreateAsync("222", "user2", Arg.Any<CancellationToken>()).Returns(user2);

        ChatMessage msg1 = CreateMessage("!test", userId: "111", username: "user1");
        ChatMessage msg2 = CreateMessage("!test", userId: "222", username: "user2");

        bool first = await _sut.HandleMessageAsync(msg1);
        first.Should().BeTrue();

        // Different user — should not be blocked
        bool second = await _sut.HandleMessageAsync(msg2);
        second.Should().BeTrue();
    }

    [Fact]
    public async Task HandleMessageAsync_NoCooldown_AllowsRepeat()
    {
        Command cmd = CreateCommand(globalCooldown: 0, userCooldown: 0);
        User user = CreateUser();
        ChatMessage msg = CreateMessage("!test");

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        bool first = await _sut.HandleMessageAsync(msg);
        bool second = await _sut.HandleMessageAsync(msg);

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Template Resolution
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleMessageAsync_ResolvesUserVariable()
    {
        ChatMessage msg = CreateMessage("!test", displayName: "CoolStreamer");
        Command cmd = CreateCommand(response: "Welcome {user}!");
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        await _chatClient.Received(1).SendMessageAsync("Welcome CoolStreamer!", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ResolvesPointsVariable()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(response: "You have {points} points!");
        User user = CreateUser(points: 4200);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        await _chatClient.Received(1).SendMessageAsync("You have 4200 points!", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ResolvesWatchtimeVariable()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(response: "Watchtime: {watchtime}");
        User user = CreateUser(watchedMinutes: 134);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        await _chatClient.Received(1).SendMessageAsync("Watchtime: 2h 14m", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ResolvesWatchtime_LessThanOneHour()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(response: "Watchtime: {watchtime}");
        User user = CreateUser(watchedMinutes: 45);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        await _chatClient.Received(1).SendMessageAsync("Watchtime: 45m", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ResolvesRandomVariable()
    {
        ChatMessage msg = CreateMessage("!test");
        Command cmd = CreateCommand(response: "Roll: {random:1:6}");
        User user = CreateUser();

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        // We can't predict the random value, so verify the message was sent and doesn't contain the template
        await _chatClient.Received(1).SendMessageAsync(
            Arg.Is<string>(s => s.StartsWith("Roll: ") && !s.Contains("{random")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ResolvesMultipleVariables()
    {
        ChatMessage msg = CreateMessage("!test", displayName: "Viewer1");
        Command cmd = CreateCommand(response: "{user} has {points} points and {count} messages");
        User user = CreateUser(points: 500, messageCount: 42);

        _commandRepo.GetByTriggerOrAliasAsync("!test", Arg.Any<CancellationToken>()).Returns(cmd);
        _userRepo.GetOrCreateAsync(msg.UserId, msg.Username, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.HandleMessageAsync(msg);

        await _chatClient.Received(1).SendMessageAsync(
            "Viewer1 has 500 points and 42 messages",
            Arg.Any<CancellationToken>());
    }
}
