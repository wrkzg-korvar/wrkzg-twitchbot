using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class CommandEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public CommandEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCommands_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/commands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

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

    [Fact]
    public async Task GetCommand_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/commands/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCommand_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/commands/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
