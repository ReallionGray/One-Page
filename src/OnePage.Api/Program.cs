using System;
using OnePage.Api;

// If running in Development and no DB provider is set, default to sqlite for local runs
var aspEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OnePage__DatabaseProvider")) && string.Equals(aspEnv, "Development", StringComparison.OrdinalIgnoreCase))
{
    Environment.SetEnvironmentVariable("OnePage__DatabaseProvider", "sqlite");
}

var app = ApiHost.Create(args);
try
{
    await ApiHost.InitializeDatabaseAsync(app.Services);
}
catch (Exception ex)
{
    // Log and continue — in some dev scenarios the DB may be transient. App will still start and surface errors on API calls.
    Console.Error.WriteLine($"Database initialization failed: {ex.Message}");
}

app.Run();

public partial class Program { }
