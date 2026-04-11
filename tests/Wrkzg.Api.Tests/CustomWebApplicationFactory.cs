using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wrkzg.Api.Security;
using Wrkzg.Api.Tests.Fakes;
using Wrkzg.Core.Interfaces;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Uses an in-memory SQLite connection (shared, kept open) and mocks/fakes for external dependencies.
/// InMemorySecureStorage is used instead of NSubstitute mock because AddInfrastructure()
/// may not register ISecureStorage on unsupported platforms (Linux CI).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>In-memory secure storage fake for testing without platform-specific credential stores.</summary>
    public InMemorySecureStorage SecureStorage { get; } = new();
    /// <summary>Mock Twitch IRC chat client.</summary>
    public ITwitchChatClient TwitchChatClient { get; } = Substitute.For<ITwitchChatClient>();
    /// <summary>Mock Broadcaster Helix API client.</summary>
    public IBroadcasterHelixClient BroadcasterHelixClient { get; } = Substitute.For<IBroadcasterHelixClient>();
    /// <summary>Mock Bot Helix API client.</summary>
    public IBotHelixClient BotHelixClient { get; } = Substitute.For<IBotHelixClient>();
    /// <summary>Mock Twitch OAuth service.</summary>
    public ITwitchOAuthService TwitchOAuthService { get; } = Substitute.For<ITwitchOAuthService>();
    /// <summary>Mock chat event broadcaster.</summary>
    public IChatEventBroadcaster ChatEventBroadcaster { get; } = Substitute.For<IChatEventBroadcaster>();
    /// <summary>Mock auth state notifier.</summary>
    public IAuthStateNotifier AuthStateNotifier { get; } = Substitute.For<IAuthStateNotifier>();

    /// <summary>Creates the factory and opens a shared in-memory SQLite connection.</summary>
    public CustomWebApplicationFactory()
    {
        // Keep an open connection so the in-memory DB persists across scopes
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <summary>Configures the web host with in-memory SQLite and mock dependencies for testing.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext registrations
            RemoveAll(services, typeof(DbContextOptions<BotDbContext>));
            RemoveAll(services, typeof(BotDbContext));

            // Add in-memory SQLite using the shared connection
            services.AddDbContext<BotDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace external dependencies with fakes/mocks
            ReplaceService<ISecureStorage>(services, SecureStorage);
            ReplaceService(services, TwitchChatClient);
            ReplaceService(services, BroadcasterHelixClient);
            ReplaceService(services, BotHelixClient);
            ReplaceService(services, TwitchOAuthService);
            ReplaceService(services, ChatEventBroadcaster);
            ReplaceService(services, AuthStateNotifier);

            // Remove hosted services (BotConnectionService, UserTrackingService)
            RemoveAll(services, typeof(Microsoft.Extensions.Hosting.IHostedService));
        });
    }

    private static void ReplaceService<T>(IServiceCollection services, T instance) where T : class
    {
        RemoveAll(services, typeof(T));
        services.AddSingleton(instance);
    }

    private static void RemoveAll(IServiceCollection services, Type serviceType)
    {
        for (int i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == serviceType)
            {
                services.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Creates an HttpClient with the API session token pre-configured as a default header.
    /// All test clients must use this to pass the ApiTokenMiddleware.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        ApiTokenService tokenService = Services.GetRequiredService<ApiTokenService>();
        client.DefaultRequestHeaders.Add("X-Wrkzg-Token", tokenService.Token);
        return client;
    }

    /// <summary>Disposes the factory and closes the shared in-memory SQLite connection.</summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
