global using ILogger = Serilog.ILogger;
using Npgsql;
using Serilog;
using Stikl.Web.Data;
using Stikl.Web.Routes;

Log.Logger = Logging.CreateConfiguration().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog();
builder
    .Services.AddSingleton(Log.Logger)
    .AddHttpClient()
    .AddSingleton<PlantSearcher>()
    .AddSingleton<NpgsqlDataSource>(_ =>
        NpgsqlDataSource.Create("Host=127.0.0.1;Username=postgres;Database=postgres")
    );
var app = builder.Build();

if (EnvironmentVariable.GetBool("SCRAPE") ?? false)
{
    var db = app.Services.GetRequiredService<NpgsqlDataSource>();
    var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

    await using var connection = await db.OpenConnectionAsync();
    var scraper = new PerenualApiScraper(
        connection,
        httpClient,
        Log.Logger,
        EnvironmentVariable.GetRequired("PERENUAL_API_KEY")
    );

    await scraper.Scrape(53);
    return;
}

app.UseStaticFiles();
app.UseSerilogRequestLogging();
RootRouter.Map(app);
app.Run();
