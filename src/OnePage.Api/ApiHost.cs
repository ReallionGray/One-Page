using OnePage.Platform;
using Microsoft.EntityFrameworkCore;

namespace OnePage.Api;

public static class ApiHost
{
    public static WebApplication Create(string[]? args = null, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        configureBuilder?.Invoke(builder);
        builder.Services.AddSingleton<InMemoryEntitlementEvaluator>();
        builder.Services.AddSingleton<IEntitlementEvaluator>(sp => sp.GetRequiredService<InMemoryEntitlementEvaluator>());
        builder.Services.AddSingleton<ITrustedApiCredentialResolver, ConfigurationApiCredentialResolver>();
        builder.Services.AddDbContext<OrganizationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("OnePage") ?? "Host=localhost;Database=onepage;Username=postgres;Password=postgres"));
        builder.Services.AddScoped<TenantContextAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());
        builder.Services.AddScoped<ITenantRepository, TenantRepository>();
        builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        var app = builder.Build();
        app.Use(async (httpContext, next) =>
        {
            if (!httpContext.Request.Path.StartsWithSegments("/api/v1/platform"))
            {
                await next(httpContext);
                return;
            }

            var credentials = httpContext.RequestServices.GetRequiredService<ITrustedApiCredentialResolver>();
            var credential = credentials.Resolve(httpContext.Request.Headers["X-API-Key"].FirstOrDefault());
            if (credential is null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Results.Problem(statusCode: 401, title: "Authentication required", detail: "A valid API key is required.").ExecuteAsync(httpContext);
                return;
            }

            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await Results.Problem(statusCode: 400, title: "Tenant selection required", detail: "X-Tenant-Id is required.").ExecuteAsync(httpContext);
                return;
            }

            if (!credential.AllowedTenantIds.Contains(tenantId.Trim()))
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await Results.Problem(statusCode: 403, title: "Tenant access denied", detail: "The API credential is not authorized for this tenant.").ExecuteAsync(httpContext);
                return;
            }

            var accessor = httpContext.RequestServices.GetRequiredService<TenantContextAccessor>();
            accessor.Current = TenantContext.Create(credential.UserId, tenantId, httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N"));
            await next(httpContext);
        });
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/api/v1/platform/entitlements/{namespace}/{*name}", CheckEntitlement);
        app.MapGet("/api/v1/platform/organization/branches/{id}", async (string id, IOrganizationRepository repository, CancellationToken ct) => Results.Ok(await repository.GetAsync<Branch>(id, ct)));
        app.MapGet("/api/v1/platform/context", (ITenantContextAccessor accessor) => Results.Ok(accessor.Current));
        return app;
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        await OrganizationPersistence.InitializeAsync(db, cancellationToken);
    }

    private static IResult CheckEntitlement(string @namespace, string name, ITenantContextAccessor accessor, IEntitlementEvaluator evaluator, HttpRequest request)
    {
        try
        {
            var context = accessor.Current ?? throw new TenantContextValidationException("tenantContext", "A tenant context is required.");
            var key = new EntitlementKey(@namespace, name);
            var requestedUsage = long.TryParse(request.Query["requestedUsage"], out var parsed) ? parsed : 0;
            var historicalRead = bool.TryParse(request.Query["historicalRead"], out var historical) && historical;
            var decision = evaluator.Evaluate(context, key, requestedUsage, historicalRead);
            return decision.Allowed ? Results.Ok(decision) : Results.Problem(statusCode: 403, title: "Entitlement denied", detail: $"Entitlement '{key}' cannot be used for this operation.", extensions: new Dictionary<string, object?>
            {
                ["code"] = "ENTITLEMENT_DENIED", ["denialReason"] = decision.DenialReason?.ToString(), ["entitlement"] = key.ToString()
            });
        }
        catch (TenantContextValidationException ex)
        {
            return Results.Problem(statusCode: 400, title: "Invalid tenant context", detail: ex.Message);
        }
    }
}
