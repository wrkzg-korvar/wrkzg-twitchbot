using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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

    public InMemorySecureStorage SecureStorage { get; } = new();
    public ITwitchChatClient TwitchChatClient { get; } = Substitute.For<ITwitchChatClient>();
    public ITwitchHelixClient TwitchHelixClient { get; } = Substitute.For<ITwitchHelixClient>();
    public ITwitchOAuthService TwitchOAuthService { get; } = Substitute.For<ITwitchOAuthService>();
    public IChatEventBroadcaster ChatEventBroadcaster { get; } = Substitute.For<IChatEventBroadcaster>();
    public IAuthStateNotifier AuthStateNotifier { get; } = Substitute.For<IAuthStateNotifier>();

    public CustomWebApplicationFactory()
    {
        // Keep an open connection so the in-memory DB persists across scopes
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

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
            ReplaceService(services, TwitchHelixClient);
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
