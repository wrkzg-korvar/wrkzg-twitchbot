using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the timed message API endpoints.</summary>
public class TimerEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public TimerEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that listing all timers returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetTimers_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/timers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that creating a timer with valid data returns HTTP 201 Created.</summary>
    [Fact]
    public async Task CreateTimer_ValidRequest_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/timers", new
        {
            name = "Test Timer",
            messages = new[] { "Hello!", "World!" },
            intervalMinutes = 10,
            minChatLines = 5,
            isEnabled = true,
            runWhenOnline = true,
            runWhenOffline = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>Verifies that creating a timer with an empty name returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task CreateTimer_EmptyName_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/timers", new
        {
            name = "",
            messages = new[] { "Hello!" },
            intervalMinutes = 10
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that deleting a non-existent timer returns HTTP 204 No Content.</summary>
    [Fact]
    public async Task DeleteTimer_NonExistent_ReturnsNoContent()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/timers/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
