using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class RoleEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RoleEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetRoles_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

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

    [Fact]
    public async Task DeleteRole_NonExistent_ReturnsNoContent()
    {
        // DeleteAsync returns NoContent even for non-existent IDs (idempotent)
        HttpResponseMessage response = await _client.DeleteAsync("/api/roles/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

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

    [Fact]
    public async Task EvaluateAll_ReturnsOk()
    {
        HttpResponseMessage response = await _client.PostAsync("/api/roles/evaluate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("usersUpdated").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetUserRoles_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/users/1/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
