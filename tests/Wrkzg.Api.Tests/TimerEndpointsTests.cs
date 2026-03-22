using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class TimerEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TimerEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTimers_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/timers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

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

    [Fact]
    public async Task DeleteTimer_NonExistent_ReturnsNoContent()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/timers/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
