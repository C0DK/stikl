using Stikl.Web.Routes;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

RootRouter.Map(app);
app.Run();
