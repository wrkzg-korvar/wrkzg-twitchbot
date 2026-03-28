using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

public class SettingsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SettingsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetSettings_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutSettings_UpdatesValues()
    {
        Dictionary<string, string> updates = new()
        {
            ["Bot.Channel"] = "testchannel"
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/settings", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
