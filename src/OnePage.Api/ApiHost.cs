using OnePage.Platform;

namespace OnePage.Api;

public static class ApiHost
{
    public static WebApplication Create(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.Services.AddSingleton<InMemoryEntitlementEvaluator>();
        builder.Services.AddSingleton<IEntitlementEvaluator>(sp => sp.GetRequiredService<InMemoryEntitlementEvaluator>());
        var app = builder.Build();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/api/v1/platform/entitlements/{namespace}/{*name}", CheckEntitlement);
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
