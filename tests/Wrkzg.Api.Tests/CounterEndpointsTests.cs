using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the counter and spam filter API endpoints.</summary>
public class CounterEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public CounterEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that listing all counters returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetCounters_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/counters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that creating a counter with valid data returns HTTP 201 Created.</summary>
    [Fact]
    public async Task CreateCounter_ReturnsCreated()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/counters", new
        {
            name = "Deaths",
            value = 0,
            responseTemplate = "{name}: {value}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>Verifies that creating a counter with an empty name returns HTTP 400 Bad Request.</summary>
    [Fact]
    public async Task CreateCounter_EmptyName_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/counters", new
        {
            name = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Verifies that fetching the spam filter configuration returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetSpamFilter_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/spam-filter");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
