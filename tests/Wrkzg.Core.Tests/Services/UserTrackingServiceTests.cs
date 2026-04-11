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

/// <summary>Tests for the UserTrackingService background service.</summary>
public class UserTrackingServiceTests
{
    private readonly IBroadcasterHelixClient _helix;
    private readonly ITwitchChatClient _chatClient;
    private readonly IChatEventBroadcaster _broadcaster;
    private readonly IUserRepository _userRepo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly ILogger<UserTrackingService> _logger;
    private readonly UserTrackingService _sut;

    /// <summary>Initializes test dependencies with NSubstitute mocks and a real service scope factory.</summary>
    public UserTrackingServiceTests()
    {
        _helix = Substitute.For<IBroadcasterHelixClient>();
        _chatClient = Substitute.For<ITwitchChatClient>();
        _broadcaster = Substitute.For<IChatEventBroadcaster>();
        _userRepo = Substitute.For<IUserRepository>();
        _settingsRepo = Substitute.For<ISettingsRepository>();
        _logger = Substitute.For<ILogger<UserTrackingService>>();

        ServiceCollection services = new();
        services.AddScoped(_ => _userRepo);
        services.AddScoped(_ => _settingsRepo);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _sut = new UserTrackingService(_helix, _chatClient, _broadcaster, scopeFactory, _logger);
    }

    /// <summary>Verifies that marking a user active does not throw.</summary>
    [Fact]
    public void MarkUserActive_TracksUser()
    {
        _sut.MarkUserActive("user123");
        _sut.MarkUserActive("user456");

        // Marking should not throw
        // The actual verification happens in TickAsync tests
    }

    /// <summary>Verifies that marking the same user active twice does not cause an error.</summary>
    [Fact]
    public void MarkUserActive_SameUserTwice_DoesNotDuplicate()
    {
        _sut.MarkUserActive("user123");
        _sut.MarkUserActive("user123");

        // Should not throw — dictionary overwrites the timestamp
    }

    /// <summary>Verifies that the service starts without errors.</summary>
    [Fact]
    public async Task StartAsync_DoesNotThrow()
    {
        await _sut.StartAsync(CancellationToken.None);

        // Service should start without errors
        // Timer is created internally
        _sut.Dispose();
    }

    /// <summary>Verifies that the service stops cleanly after being started.</summary>
    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        await _sut.StartAsync(CancellationToken.None);
        await _sut.StopAsync(CancellationToken.None);

        // Clean shutdown
    }
}
