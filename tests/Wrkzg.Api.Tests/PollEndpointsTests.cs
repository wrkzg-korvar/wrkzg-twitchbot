using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class PollEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PollEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetActive_NoPoll_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

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

    [Fact]
    public async Task EndPoll_NoPoll_ReturnsBadRequest()
    {
        // Ensure no active poll: end any existing one first, then try again
        await _client.PostAsync("/api/polls/end", null);
        await _client.PostAsync("/api/polls/cancel", null);

        HttpResponseMessage response = await _client.PostAsync("/api/polls/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPollById_NonExistent_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTemplates_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/polls/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetTemplate_UnknownKey_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.PostAsync("/api/polls/templates/reset/unknown.key", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetTemplate_KnownKey_ReturnsOk()
    {
        HttpResponseMessage response = await _client.PostAsync("/api/polls/templates/reset/poll.announce.start", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
