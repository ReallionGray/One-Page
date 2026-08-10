using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnePage.Api;
using OnePage.Platform;

namespace OnePage.Platform.Tests;

public class ApiIntegrationTests
{
    [Fact] public async Task Health_endpoint_is_available()
    {
        await using var host = await StartHost();
        using var response = await host.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] public async Task Api_returns_denial_from_real_evaluator_for_missing_entitlement()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/entitlements/module/payroll");
        request.Headers.Add("X-API-Key", "key-1"); request.Headers.Add("X-Tenant-Id", "tenant-1"); request.Headers.Add("X-Correlation-Id", "corr-1");
        using var response = await host.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("ENTITLEMENT_DENIED", content);
    }

    [Fact] public async Task Api_propagates_tenant_context_to_scoped_platform_services()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        request.Headers.Add("X-API-Key", "key-1"); request.Headers.Add("X-Tenant-Id", "tenant-1"); request.Headers.Add("X-User-Id", "forged-user"); request.Headers.Add("X-Correlation-Id", "corr-9");
        using var response = await host.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tenant-1", body);
        Assert.Contains("user-1", body);
        Assert.DoesNotContain("forged-user", body);
    }

    [Fact]
    public async Task Api_rejects_missing_or_invalid_api_key()
    {
        await using var host = await StartHost();
        using var missing = await host.Client.GetAsync("/api/v1/platform/context");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);

        using var invalid = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        invalid.Headers.Add("X-API-Key", "invalid"); invalid.Headers.Add("X-Tenant-Id", "tenant-1");
        using var response = await host.Client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_denies_cross_tenant_selection_for_valid_credential()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        request.Headers.Add("X-API-Key", "key-1"); request.Headers.Add("X-Tenant-Id", "tenant-2");
        using var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Startup_initializer_uses_registered_database_context()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<OrganizationDbContext>(options => options.UseSqlite(connection));
        await using var provider = services.BuildServiceProvider();
        await ApiHost.InitializeDatabaseAsync(provider);
        Assert.True(await provider.GetRequiredService<OrganizationDbContext>().Database.CanConnectAsync());
    }

    private static async Task<RunningHost> StartHost()
    {
        var app = ApiHost.Create(configureBuilder: builder => builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OnePage:ApiCredentials:key-1:UserId"] = "user-1",
            ["OnePage:ApiCredentials:key-1:TenantIds:0"] = "tenant-1"
        }));
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        app.Urls.Add($"http://127.0.0.1:{port}");
        await app.StartAsync();
        var address = app.Urls.First();
        return new RunningHost(app, new HttpClient { BaseAddress = new Uri(address) });
    }

    private sealed class RunningHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public async ValueTask DisposeAsync() { Client.Dispose(); await app.StopAsync(); await app.DisposeAsync(); }
    }
}
