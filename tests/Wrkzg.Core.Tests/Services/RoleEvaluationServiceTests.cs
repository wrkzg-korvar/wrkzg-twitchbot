using System.Collections.Generic;
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

public class RoleEvaluationServiceTests
{
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRepository _userRepo;
    private readonly RoleEvaluationService _sut;

    public RoleEvaluationServiceTests()
    {
        _roleRepo = Substitute.For<IRoleRepository>();
        _userRepo = Substitute.For<IUserRepository>();

        ServiceCollection services = new();
        services.AddScoped(_ => _roleRepo);
        services.AddScoped(_ => _userRepo);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _sut = new RoleEvaluationService(
            scopeFactory,
            Substitute.For<ILogger<RoleEvaluationService>>());
    }

    private static User CreateUser(
        int id = 1,
        long points = 500,
        int watchedMinutes = 600,
        int messageCount = 100,
        bool isSubscriber = false,
        bool hasFollowDate = true)
    {
        return new User
        {
            Id = id,
            TwitchId = "12345",
            Username = "testuser",
            DisplayName = "TestUser",
            Points = points,
            WatchedMinutes = watchedMinutes,
            MessageCount = messageCount,
            IsSubscriber = isSubscriber,
            FollowDate = hasFollowDate ? System.DateTimeOffset.UtcNow.AddDays(-30) : null
        };
    }

    private static Role CreateRole(
        int id = 1,
        string name = "Elite",
        int priority = 10,
        int? minWatchedMinutes = null,
        long? minPoints = null,
        int? minMessages = null,
        bool? mustBeSubscriber = null)
    {
        return new Role
        {
            Id = id,
            Name = name,
            Priority = priority,
            AutoAssign = new RoleAutoAssignCriteria
            {
                MinWatchedMinutes = minWatchedMinutes,
                MinPoints = minPoints,
                MinMessages = minMessages,
                MustBeSubscriber = mustBeSubscriber
            }
        };
    }

    [Fact]
    public async Task EvaluateUser_AssignsRole_WhenCriteriaMet()
    {
        // Arrange: User has 600 min watch time, role requires 300 min
        User user = CreateUser(watchedMinutes: 600);
        Role role = CreateRole(minWatchedMinutes: 300);

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role>());

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeTrue();
        await _roleRepo.Received(1).AssignRoleAsync(1, role.Id, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateUser_RemovesAutoRole_WhenCriteriaNotMet()
    {
        // Arrange: User has 100 min, role requires 300 min, user has role (auto-assigned)
        User user = CreateUser(watchedMinutes: 100);
        Role role = CreateRole(minWatchedMinutes: 300);

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.IsAutoAssignedAsync(1, role.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeTrue();
        await _roleRepo.Received(1).RemoveRoleAsync(1, role.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateUser_KeepsManualRole_WhenCriteriaNotMet()
    {
        // Arrange: User has 100 min, role requires 300 min, but role was manually assigned
        User user = CreateUser(watchedMinutes: 100);
        Role role = CreateRole(minWatchedMinutes: 300);

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.IsAutoAssignedAsync(1, role.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeFalse();
        await _roleRepo.DidNotReceive().RemoveRoleAsync(1, role.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateUser_DoesNotAssign_WhenNotAllCriteriaMet()
    {
        // Arrange: User meets watch time but not points requirement
        User user = CreateUser(watchedMinutes: 600, points: 50);
        Role role = CreateRole(minWatchedMinutes: 300, minPoints: 500);

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role>());

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeFalse();
        await _roleRepo.DidNotReceive().AssignRoleAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateUser_SkipsRole_WithNullAutoAssign()
    {
        // Arrange: Role without auto-assign criteria (manual only)
        User user = CreateUser();
        Role manualRole = new()
        {
            Id = 1,
            Name = "VIP",
            Priority = 50,
            AutoAssign = null
        };

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { manualRole });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role>());

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeFalse();
        await _roleRepo.DidNotReceive().AssignRoleAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateUser_ChecksSubscriberCriteria()
    {
        // Arrange: Role requires subscriber, user is not subscriber
        User user = CreateUser(isSubscriber: false);
        Role role = CreateRole(mustBeSubscriber: true);

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role>());

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateUser_AssignsMultipleRoles()
    {
        // Arrange: User qualifies for two roles
        User user = CreateUser(watchedMinutes: 1000, points: 5000, messageCount: 500);
        Role role1 = CreateRole(id: 1, name: "Regular", minWatchedMinutes: 60);
        Role role2 = CreateRole(id: 2, name: "Elite", minPoints: 1000);

        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role1, role2 });
        _roleRepo.GetUserRolesAsync(1, Arg.Any<CancellationToken>()).Returns(new List<Role>());

        // Act
        bool changed = await _sut.EvaluateUserAsync(1);

        // Assert
        changed.Should().BeTrue();
        await _roleRepo.Received(1).AssignRoleAsync(1, 1, true, Arg.Any<CancellationToken>());
        await _roleRepo.Received(1).AssignRoleAsync(1, 2, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateUser_ReturnsfalseForNonexistentUser()
    {
        _userRepo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((User?)null);

        bool changed = await _sut.EvaluateUserAsync(999);

        changed.Should().BeFalse();
    }
}
