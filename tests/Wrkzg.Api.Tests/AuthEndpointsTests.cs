using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSetupStatus_NoCredentials_ReturnsIncomplete()
    {
        // InMemorySecureStorage starts empty — no credentials, no tokens
        HttpResponseMessage response = await _client.GetAsync("/auth/setup-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"setupComplete\":false");
    }

    [Fact]
    public async Task GetAuthStatus_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
