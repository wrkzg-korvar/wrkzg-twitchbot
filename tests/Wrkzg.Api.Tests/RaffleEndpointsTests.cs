using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class RaffleEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RaffleEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActive_NoRaffle_ReturnsNotFound()
    {
        // Ensure no active raffle
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.GetAsync("/api/raffles/active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRaffle_ValidRequest_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/raffles", new
        {
            title = "Test Raffle",
            keyword = "win",
            durationSeconds = (int?)null,
            maxEntries = (int?)null,
            createdBy = "TestUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateRaffle_EmptyTitle_ReturnsBadRequest()
    {
        // Cancel any existing raffle first
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/raffles", new
        {
            title = "",
            createdBy = "TestUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Draw_NoRaffle_ReturnsBadRequest()
    {
        // Ensure no active raffle
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/raffles/draw", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTemplates_ReturnsAllKeys()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/raffles/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Accept_NoPending_ReturnsBadRequest()
    {
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/raffles/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Redraw_NoPending_ReturnsBadRequest()
    {
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/raffles/redraw", new
        {
            reason = "Not present"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EndRaffle_NoRaffle_ReturnsBadRequest()
    {
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/raffles/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChatRecent_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/chat/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChatRecent_FilterByUserId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/chat/recent?userId=12345");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
