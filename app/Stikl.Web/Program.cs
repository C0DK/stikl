using Stikl.Web.Routes;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseStaticFiles();
RootRouter.Map(app);
app.Run();
