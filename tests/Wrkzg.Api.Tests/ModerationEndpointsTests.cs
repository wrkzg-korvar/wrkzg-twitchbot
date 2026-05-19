using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>Integration tests for the moderation API endpoints.</summary>
public class ModerationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Initializes the test with an authenticated HTTP client.</summary>
    public ModerationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    /// <summary>GET /api/moderation/log returns an empty array on a fresh database.</summary>
    [Fact]
    public async Task GetLog_EmptyDatabase_ReturnsEmptyArray()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/moderation/log");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    /// <summary>GET /api/moderation/log/{userId} returns an empty array for unknown users.</summary>
    [Fact]
    public async Task GetLogByUser_UnknownUser_ReturnsEmptyArray()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/moderation/log/nonexistent123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    /// <summary>GET /api/moderation/viewers returns OK (empty when no active users).</summary>
    [Fact]
    public async Task GetViewers_NoActiveUsers_ReturnsEmptyArray()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/moderation/viewers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }

    /// <summary>POST /api/moderation/timeout with missing TwitchUserId returns 400.</summary>
    [Fact]
    public async Task Timeout_MissingUserId_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/moderation/timeout",
            new { twitchUserId = "", durationSeconds = 60 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>POST /api/moderation/timeout with invalid duration returns 400.</summary>
    [Fact]
    public async Task Timeout_InvalidDuration_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/moderation/timeout",
            new { twitchUserId = "user123", durationSeconds = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>POST /api/moderation/timeout with duration exceeding 14 days returns 400.</summary>
    [Fact]
    public async Task Timeout_DurationExceeds14Days_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/moderation/timeout",
            new { twitchUserId = "user123", durationSeconds = 1_209_601 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>POST /api/moderation/ban with missing TwitchUserId returns 400.</summary>
    [Fact]
    public async Task Ban_MissingUserId_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/moderation/ban",
            new { twitchUserId = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>POST /api/moderation/shoutout with missing TwitchUserId returns 400.</summary>
    [Fact]
    public async Task Shoutout_MissingUserId_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/moderation/shoutout",
            new { twitchUserId = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>DELETE /api/moderation/log/cleanup succeeds and returns the deleted count.</summary>
    [Fact]
    public async Task LogCleanup_EmptyDatabase_ReturnsZeroDeleted()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/moderation/log/cleanup");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"deleted\":0");
    }

    /// <summary>POST /api/moderation/timeout without broadcaster token returns 401.</summary>
    [Fact]
    public async Task Timeout_NoBroadcasterToken_ReturnsUnauthorized()
    {
        // The factory's SecureStorage is empty by default — no broadcaster token.
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/moderation/timeout",
            new { twitchUserId = "user123", durationSeconds = 60 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
