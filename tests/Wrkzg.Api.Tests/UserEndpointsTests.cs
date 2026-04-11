using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the user management API endpoints.</summary>
public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that listing users on an empty database returns a paginated result with empty items.</summary>
    [Fact]
    public async Task GetUsers_EmptyDatabase_ReturnsPaginatedEmpty()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
        body.Should().Contain("\"totalCount\":0");
    }

    /// <summary>Verifies that fetching a non-existent user returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task GetUser_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/users/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
