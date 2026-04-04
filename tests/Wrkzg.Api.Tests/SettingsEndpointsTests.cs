using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Tests for the settings API endpoints.</summary>
public class SettingsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public SettingsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>Verifies that fetching all settings returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetSettings_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Verifies that updating settings via PUT returns HTTP 200 OK.</summary>
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
