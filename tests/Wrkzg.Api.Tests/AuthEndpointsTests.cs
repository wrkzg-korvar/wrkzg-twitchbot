using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Api.Tests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSetupStatus_NoCredentials_ReturnsIncomplete()
    {
        _factory.SecureStorage.HasCredentialsAsync(Arg.Any<CancellationToken>()).Returns(false);
        _factory.SecureStorage.LoadTokensAsync(TokenType.Bot, Arg.Any<CancellationToken>()).Returns((TwitchTokens?)null);
        _factory.SecureStorage.LoadTokensAsync(TokenType.Broadcaster, Arg.Any<CancellationToken>()).Returns((TwitchTokens?)null);

        HttpResponseMessage response = await _client.GetAsync("/auth/setup-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"setupComplete\":false");
    }

    [Fact]
    public async Task GetAuthStatus_ReturnsOk()
    {
        _factory.SecureStorage.LoadTokensAsync(Arg.Any<TokenType>(), Arg.Any<CancellationToken>())
            .Returns((TwitchTokens?)null);

        HttpResponseMessage response = await _client.GetAsync("/auth/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
