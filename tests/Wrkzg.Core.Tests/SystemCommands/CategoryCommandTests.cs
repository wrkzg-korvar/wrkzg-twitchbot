using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.SystemCommands;
using Xunit;

namespace Wrkzg.Core.Tests.SystemCommands;

/// <summary>
/// Tests for the CategoryCommand system command: trigger/alias contract, moderator gating,
/// argument parsing and Helix interaction.
/// </summary>
public class CategoryCommandTests
{
    private readonly IBroadcasterHelixClient _helix;
    private readonly ISecureStorage _storage;
    private readonly ITwitchOAuthService _oauth;
    private readonly CategoryCommand _sut;

    /// <summary>Initializes test dependencies with NSubstitute mocks and a real service scope factory.</summary>
    public CategoryCommandTests()
    {
        _helix = Substitute.For<IBroadcasterHelixClient>();
        _storage = Substitute.For<ISecureStorage>();
        _oauth = Substitute.For<ITwitchOAuthService>();

        ServiceCollection services = new();
        services.AddScoped(_ => _helix);
        services.AddScoped(_ => _storage);
        services.AddScoped(_ => _oauth);
        ServiceProvider provider = services.BuildServiceProvider();

        _sut = new CategoryCommand(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static ChatMessage CreateMessage(string content, bool isMod = true, bool isBroadcaster = false)
    {
        return new ChatMessage(
            UserId: "12345",
            Username: "testuser",
            DisplayName: "TestUser",
            Content: content,
            IsModerator: isMod,
            IsSubscriber: false,
            IsBroadcaster: isBroadcaster,
            Timestamp: DateTimeOffset.UtcNow);
    }

    private void GivenConnectedBroadcaster(string userId = "999")
    {
        _storage.LoadTokensAsync(TokenType.Broadcaster, Arg.Any<CancellationToken>())
            .Returns(new TwitchTokens { AccessToken = "token" });
        _oauth.ValidateTokenAsync("token", Arg.Any<CancellationToken>())
            .Returns(new TwitchTokenValidation { UserId = userId });
    }

    /// <summary>The primary trigger is !category — the command was renamed away from !game.</summary>
    [Fact]
    public void Trigger_IsCategory()
    {
        _sut.Trigger.Should().Be("!category");
    }

    /// <summary>
    /// Regression guard for the !game removal: the only alias is !ctgy and !game must not
    /// reappear as either the trigger or an alias.
    /// </summary>
    [Fact]
    public void Aliases_ContainOnlyCtgy_AndNeverGame()
    {
        _sut.Aliases.Should().Equal("!ctgy");
        _sut.Aliases.Should().NotContain("!game");
        _sut.Trigger.Should().NotBe("!game");
    }

    /// <summary>The user-facing description advertises the new !category usage, not !game.</summary>
    [Fact]
    public void Description_ReferencesCategoryNotGame()
    {
        _sut.Description.Should().Contain("!category");
        _sut.Description.Should().NotContain("!game");
    }

    /// <summary>Regular viewers cannot change the category and get no response at all.</summary>
    [Fact]
    public async Task ExecuteAsync_NonModerator_ReturnsNull()
    {
        string? result = await _sut.ExecuteAsync(CreateMessage("!category Crimson Desert", isMod: false));

        result.Should().BeNull();
        await _helix.DidNotReceiveWithAnyArgs().ModifyChannelInfoAsync(default!, default, default);
    }

    /// <summary>The broadcaster is allowed even without the moderator flag.</summary>
    [Fact]
    public async Task ExecuteAsync_Broadcaster_IsAllowed()
    {
        GivenConnectedBroadcaster();
        _helix.GetGameByNameAsync("Crimson Desert", Arg.Any<CancellationToken>())
            .Returns(new TwitchGameInfo { Id = "42", Name = "Crimson Desert" });
        _helix.ModifyChannelInfoAsync("999", null, "42", Arg.Any<CancellationToken>()).Returns(true);

        string? result = await _sut.ExecuteAsync(
            CreateMessage("!category Crimson Desert", isMod: false, isBroadcaster: true));

        result.Should().Be("Category changed to: Crimson Desert");
    }

    /// <summary>Missing arguments produce usage help that names the new trigger.</summary>
    [Theory]
    [InlineData("!category")]
    [InlineData("!category   ")]
    [InlineData("!ctgy")]
    public async Task ExecuteAsync_WithoutArguments_ReturnsUsageForCategory(string content)
    {
        string? result = await _sut.ExecuteAsync(CreateMessage(content));

        result.Should().Be("Usage: !category Category Name");
        await _helix.DidNotReceiveWithAnyArgs().GetGameByNameAsync(default!);
    }

    /// <summary>Invoking via the !ctgy alias parses arguments identically to the primary trigger.</summary>
    [Fact]
    public async Task ExecuteAsync_ViaCtgyAlias_ResolvesSameCategory()
    {
        GivenConnectedBroadcaster();
        _helix.GetGameByNameAsync("Crimson Desert", Arg.Any<CancellationToken>())
            .Returns(new TwitchGameInfo { Id = "42", Name = "Crimson Desert" });
        _helix.ModifyChannelInfoAsync("999", null, "42", Arg.Any<CancellationToken>()).Returns(true);

        string? result = await _sut.ExecuteAsync(CreateMessage("!ctgy Crimson Desert"));

        result.Should().Be("Category changed to: Crimson Desert");
        await _helix.Received(1).ModifyChannelInfoAsync("999", null, "42", Arg.Any<CancellationToken>());
    }

    /// <summary>Without a connected broadcaster token the command reports the missing connection.</summary>
    [Fact]
    public async Task ExecuteAsync_BroadcasterNotConnected_ReturnsNotConnected()
    {
        _storage.LoadTokensAsync(TokenType.Broadcaster, Arg.Any<CancellationToken>())
            .Returns((TwitchTokens?)null);

        string? result = await _sut.ExecuteAsync(CreateMessage("!category Crimson Desert"));

        result.Should().Be("Broadcaster not connected.");
    }

    /// <summary>An unknown category name is reported back instead of being silently ignored.</summary>
    [Fact]
    public async Task ExecuteAsync_UnknownCategory_ReturnsNotFound()
    {
        GivenConnectedBroadcaster();
        _helix.GetGameByNameAsync("Nope", Arg.Any<CancellationToken>()).Returns((TwitchGameInfo?)null);

        string? result = await _sut.ExecuteAsync(CreateMessage("!category Nope"));

        result.Should().Be("Category 'Nope' not found. Check the name.");
        await _helix.DidNotReceiveWithAnyArgs().ModifyChannelInfoAsync(default!, default, default);
    }

    /// <summary>A failing Helix update surfaces a permission hint rather than a false success.</summary>
    [Fact]
    public async Task ExecuteAsync_HelixUpdateFails_ReturnsFailureHint()
    {
        GivenConnectedBroadcaster();
        _helix.GetGameByNameAsync("Crimson Desert", Arg.Any<CancellationToken>())
            .Returns(new TwitchGameInfo { Id = "42", Name = "Crimson Desert" });
        _helix.ModifyChannelInfoAsync("999", null, "42", Arg.Any<CancellationToken>()).Returns(false);

        string? result = await _sut.ExecuteAsync(CreateMessage("!category Crimson Desert"));

        result.Should().Be("Failed to change category. Check permissions.");
    }

    /// <summary>Only the category is changed; the stream title is explicitly left untouched.</summary>
    [Fact]
    public async Task ExecuteAsync_LeavesTitleUnchanged()
    {
        GivenConnectedBroadcaster();
        _helix.GetGameByNameAsync("Crimson Desert", Arg.Any<CancellationToken>())
            .Returns(new TwitchGameInfo { Id = "42", Name = "Crimson Desert" });
        _helix.ModifyChannelInfoAsync("999", null, "42", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.ExecuteAsync(CreateMessage("!category Crimson Desert"));

        await _helix.Received(1).ModifyChannelInfoAsync("999", null, "42", Arg.Any<CancellationToken>());
    }
}
