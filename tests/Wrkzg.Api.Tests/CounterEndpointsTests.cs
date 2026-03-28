using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class CounterEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CounterEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetCounters_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/counters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

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

    [Fact]
    public async Task CreateCounter_EmptyName_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/counters", new
        {
            name = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSpamFilter_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/spam-filter");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
