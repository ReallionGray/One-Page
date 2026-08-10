using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using OnePage.Api;

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
        request.Headers.Add("X-User-Id", "user-1"); request.Headers.Add("X-Tenant-Id", "tenant-1"); request.Headers.Add("X-Correlation-Id", "corr-1");
        using var response = await host.Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("ENTITLEMENT_DENIED", content);
    }

    [Fact] public async Task Api_propagates_tenant_context_to_scoped_platform_services()
    {
        await using var host = await StartHost();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/platform/context");
        request.Headers.Add("X-User-Id", "user-9"); request.Headers.Add("X-Tenant-Id", "tenant-9"); request.Headers.Add("X-Correlation-Id", "corr-9");
        using var response = await host.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tenant-9", body);
        Assert.Contains("user-9", body);
    }

    private static async Task<RunningHost> StartHost()
    {
        var app = ApiHost.Create();
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
