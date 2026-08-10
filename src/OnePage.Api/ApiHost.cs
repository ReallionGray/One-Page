using OnePage.Platform;
using Microsoft.EntityFrameworkCore;

namespace OnePage.Api;

public static class ApiHost
{
    public static WebApplication Create(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.Services.AddSingleton<InMemoryEntitlementEvaluator>();
        builder.Services.AddSingleton<IEntitlementEvaluator>(sp => sp.GetRequiredService<InMemoryEntitlementEvaluator>());
        builder.Services.AddDbContext<OrganizationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("OnePage") ?? "Host=localhost;Database=onepage;Username=postgres;Password=postgres"));
        builder.Services.AddScoped<TenantContextAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor>(sp => sp.GetRequiredService<TenantContextAccessor>());
        builder.Services.AddScoped<ITenantRepository, TenantRepository>();
        builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        var app = builder.Build();
        app.Use(async (httpContext, next) =>
        {
            var accessor = httpContext.RequestServices.GetRequiredService<TenantContextAccessor>();
            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenant) && !string.IsNullOrWhiteSpace(tenant))
                accessor.Current = TenantContext.Create(httpContext.Request.Headers["X-User-Id"].FirstOrDefault() ?? "api-user", tenant.FirstOrDefault(), httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N"));
            await next(httpContext);
        });
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/api/v1/platform/entitlements/{namespace}/{*name}", CheckEntitlement);
        app.MapGet("/api/v1/platform/organization/branches/{id}", async (string id, IOrganizationRepository repository, CancellationToken ct) => Results.Ok(await repository.GetAsync<Branch>(id, ct)));
        app.MapGet("/api/v1/platform/context", (ITenantContextAccessor accessor) => Results.Ok(accessor.Current));
        return app;
    }

    private static IResult CheckEntitlement(string @namespace, string name, HttpRequest request, IEntitlementEvaluator evaluator)
    {
        try
        {
            var context = TenantContext.Create(request.Headers["X-User-Id"].FirstOrDefault(), request.Headers["X-Tenant-Id"].FirstOrDefault(), request.Headers["X-Correlation-Id"].FirstOrDefault());
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
