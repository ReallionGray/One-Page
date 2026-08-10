using OnePage.Api;

var app = ApiHost.Create(args);
await ApiHost.InitializeDatabaseAsync(app.Services);
app.Run();

public partial class Program { }
