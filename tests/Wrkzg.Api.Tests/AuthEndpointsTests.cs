using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the authentication API endpoints.</summary>
public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that setup status returns incomplete when no credentials are stored.</summary>
    [Fact]
    public async Task GetSetupStatus_NoCredentials_ReturnsIncomplete()
    {
        // InMemorySecureStorage starts empty — no credentials, no tokens
        HttpResponseMessage response = await _client.GetAsync("/auth/setup-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"setupComplete\":false");
    }

    /// <summary>Verifies that the auth status endpoint returns OK.</summary>
    [Fact]
    public async Task GetAuthStatus_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
