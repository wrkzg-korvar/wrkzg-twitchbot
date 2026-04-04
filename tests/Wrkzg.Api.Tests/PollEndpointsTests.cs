using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the poll management API endpoints.</summary>
public class PollEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public PollEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that requesting the active poll when none exists returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task GetActive_NoPoll_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that creating a poll with valid data returns HTTP 201 Created.</summary>
    [Fact]
    public async Task CreatePoll_ValidRequest_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/polls", new
        {
            question = "Test poll?",
            options = new[] { "Option A", "Option B" },
            durationSeconds = 60,
            createdBy = "TestUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>Verifies that creating a poll with fewer than two options returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task CreatePoll_TooFewOptions_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/polls", new
        {
            question = "Bad poll?",
            options = new[] { "Only one" },
            durationSeconds = 60,
            createdBy = "TestUser"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that ending a poll when none is active returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task EndPoll_NoPoll_ReturnsBadRequest()
    {
        // Ensure no active poll: end any existing one first, then try again
        await _client.PostAsync("/api/polls/end", null);
        await _client.PostAsync("/api/polls/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/polls/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that fetching poll history returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that fetching a non-existent poll by ID returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task GetPollById_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that fetching poll templates returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetTemplates_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that resetting a template with an unknown key returns HTTP 404 Not Found.</summary>
    [Fact]
    public async Task ResetTemplate_UnknownKey_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.PostAsync("/api/polls/templates/reset/unknown.key", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Verifies that resetting a template with a known key returns HTTP 200 OK.</summary>
    [Fact]
    public async Task ResetTemplate_KnownKey_ReturnsOk()
    {
        HttpResponseMessage response = await _client.PostAsync("/api/polls/templates/reset/poll.announce.start", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
