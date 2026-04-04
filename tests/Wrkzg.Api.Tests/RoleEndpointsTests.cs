using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the role management API endpoints.</summary>
public class RoleEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public RoleEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that listing all roles returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetRoles_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that creating a role with valid data returns HTTP 201 Created with correct properties.</summary>
    [Fact]
    public async Task CreateRole_ValidRequest_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = "Elite Viewer",
            priority = 10,
            color = "#8b5cf6"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Elite Viewer");
        body.GetProperty("priority").GetInt32().Should().Be(10);
        body.GetProperty("color").GetString().Should().Be("#8b5cf6");
    }

    /// <summary>Verifies that creating a role with an empty name returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task CreateRole_EmptyName_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = "",
            priority = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that creating a role with auto-assign criteria returns HTTP 201 Created.</summary>
    [Fact]
    public async Task CreateRole_WithAutoAssign_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = "Regular",
            priority = 5,
            autoAssign = new
            {
                minWatchedMinutes = 600,
                minPoints = 100
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>Verifies that deleting a non-existent role returns HTTP 204 No Content (idempotent).</summary>
    [Fact]
    public async Task DeleteRole_NonExistent_ReturnsNoContent()
    {
        // DeleteAsync returns NoContent even for non-existent IDs (idempotent)
        HttpResponseMessage response = await _client.DeleteAsync("/api/roles/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>Verifies that fetching users assigned to a role returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetRoleUsers_ReturnsOk()
    {
        // Create a role first
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/roles", new
        {
            name = "TestRoleUsers",
            priority = 1
        });
        JsonElement role = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        int roleId = role.GetProperty("id").GetInt32();

        // Get users with this role (empty list expected)
        HttpResponseMessage response = await _client.GetAsync($"/api/roles/{roleId}/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that triggering role evaluation for all users returns HTTP 200 OK.</summary>
    [Fact]
    public async Task EvaluateAll_ReturnsOk()
    {
        HttpResponseMessage response = await _client.PostAsync("/api/roles/evaluate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("usersUpdated").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    /// <summary>Verifies that fetching roles for a specific user returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetUserRoles_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/users/1/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
