using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class ChannelPointEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChannelPointEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetChannelPointHandlers_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/channel-points");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateChannelPointHandler_ValidRequest_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/channel-points", new
        {
            twitchRewardId = "test-reward-123",
            title = "Test Reward",
            cost = 500,
            actionType = 0,
            actionPayload = "{user} redeemed a test reward!",
            autoFulfill = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("twitchRewardId").GetString().Should().Be("test-reward-123");
        body.GetProperty("title").GetString().Should().Be("Test Reward");
        body.GetProperty("cost").GetInt32().Should().Be(500);
        body.GetProperty("isEnabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateChannelPointHandler_EmptyRewardId_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/channel-points", new
        {
            twitchRewardId = "",
            title = "Test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateChannelPointHandler_DuplicateRewardId_ReturnsBadRequest()
    {
        // Create first handler
        await _client.PostAsJsonAsync("/api/channel-points", new
        {
            twitchRewardId = "duplicate-reward-456",
            title = "First",
            actionPayload = "test"
        });

        // Try to create duplicate
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/channel-points", new
        {
            twitchRewardId = "duplicate-reward-456",
            title = "Second",
            actionPayload = "test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteChannelPointHandler_ReturnsNoContent()
    {
        // Create a handler first
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/channel-points", new
        {
            twitchRewardId = "delete-test-789",
            title = "To Delete",
            actionPayload = "test"
        });
        JsonElement body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        int id = body.GetProperty("id").GetInt32();

        // Delete it
        HttpResponseMessage response = await _client.DeleteAsync($"/api/channel-points/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetChannelPointRewards_ReturnsOk()
    {
        // Helix client is mocked via NSubstitute — returns empty list by default
        HttpResponseMessage response = await _client.GetAsync("/api/channel-points/rewards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
