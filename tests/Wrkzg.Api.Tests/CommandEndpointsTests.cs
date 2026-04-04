using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the command management API endpoints.</summary>
public class CommandEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public CommandEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that listing all commands returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetCommands_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/commands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that creating a command with valid data returns HTTP 201 Created.</summary>
    [Fact]
    public async Task CreateCommand_ValidRequest_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/commands", new
        {
            trigger = "!hello",
            responseTemplate = "Hello {user}!",
            permissionLevel = 0,
            globalCooldownSeconds = 5,
            userCooldownSeconds = 10
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>Verifies that creating a command with an empty trigger returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task CreateCommand_MissingTrigger_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/commands", new
        {
            trigger = "",
            responseTemplate = "Hello!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that creating a command whose trigger lacks the required prefix returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task CreateCommand_TriggerWithoutPrefix_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/commands", new
        {
            trigger = "hello",
            responseTemplate = "Hello!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that fetching a non-existent command returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task GetCommand_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/commands/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that deleting a non-existent command returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task DeleteCommand_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/commands/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
