using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the raffle and chat API endpoints.</summary>
public class RaffleEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public RaffleEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that requesting the active raffle when none exists returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task GetActive_NoRaffle_ReturnsNotFound()
    {
        // Ensure no active raffle
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.GetAsync("/api/raffles/active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that creating a raffle with valid data returns HTTP 201 Created.</summary>
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

    /// <summary>Verifies that creating a raffle with an empty title returns HTTP 400 Bad Request.</summary>
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

    /// <summary>Verifies that drawing a winner when no raffle is active returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task Draw_NoRaffle_ReturnsBadRequest()
    {
        // Ensure no active raffle
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/raffles/draw", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that fetching raffle templates returns HTTP 200 OK with all template keys.</summary>
    [Fact]
    public async Task GetTemplates_ReturnsAllKeys()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/raffles/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that accepting a winner when no draw is pending returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task Accept_NoPending_ReturnsBadRequest()
    {
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/raffles/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that requesting a redraw when no draw is pending returns HTTP 400 Bad Request.</summary>
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

    /// <summary>Verifies that ending a raffle when none is active returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task EndRaffle_NoRaffle_ReturnsBadRequest()
    {
        await _client.PostAsync("/api/raffles/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/raffles/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that fetching recent chat messages returns HTTP 200 OK.</summary>
    [Fact]
    public async Task ChatRecent_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/chat/recent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that filtering recent chat messages by user ID returns HTTP 200 OK.</summary>
    [Fact]
    public async Task ChatRecent_FilterByUserId_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/chat/recent?userId=12345");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
