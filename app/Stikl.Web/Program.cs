global using ILogger = Serilog.ILogger;
using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using Serilog;
using Stikl.Web.Data;
using Stikl.Web.DataAccess;
using Stikl.Web.Routes;

Log.Logger = Logging.CreateConfiguration().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

FlurlHttp.Clients.WithDefaults(builder =>
    builder.WithSettings(s =>
        s.JsonSerializer = new DefaultJsonSerializer(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                RespectRequiredConstructorParameters = true,
                RespectNullableAnnotations = true,
            }
        )
    )
);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(Log.Logger);
builder.Services.AddHttpClient();
builder.Services.AddTransient<ToastHandler>();
builder.Services.AddTransient<PlantSearcher>();
builder.Services.AddTransient<SpeciesSource>();
builder.Services.AddTransient<UserEventWriter>();
builder.Services.AddTransient<UserSource>();
builder.Services.AddTransient<NpgsqlConnection>(s =>
    s.GetRequiredService<NpgsqlDataSource>().OpenConnection()
);
builder.Services.AddTransient<ChatStore>(s => new ChatStore(
    s.GetRequiredService<NpgsqlConnection>(),
    s.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new NullReferenceException()
));
builder.Services.AddSingleton<LocationIQClient>();
builder.Services.AddSingleton<ChatBroker>();
builder.Services.AddSingleton(s => new LocationIQClient(
    EnvironmentVariable.GetRequired("LOCATION_IQ_API_KEY")
));
builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
    NpgsqlDataSource.Create("Host=127.0.0.1;Username=postgres;Database=postgres")
);

builder.Services.AddHostedService(s =>
    // this ensures that we get the same as registered above
    s.GetRequiredService<ChatBroker>()
);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});
builder
    .Services.AddAuthorization()
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // TODO: handle never Remember Me and stuff
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
        // TODO: add
        options.AccessDeniedPath = "/forbidden/";
        options.LoginPath = "/auth"; // TODO: redirect doesnt fully work with htmx!
        options.LogoutPath = "/auth/logout";
    });
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
        EnvironmentVariable.GetRequired("PERENUAL_API_KEY"),
        EnvironmentVariable.GetRequired("IMAGE_FOLDER")
    );

    // TODO Check if we missing images + gaps in ids potentially maybe
    await scraper.Scrape(530);
    return;
}

// TODO: error middleware!
// TODO: also surface csrf errors!
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseSession();
app.UseSerilogRequestLogging();
RootRouter.Map(app);
app.Run();
