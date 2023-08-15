using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Server.Builders;
using Server.Common;
using Server.Crawlers;
using Server.Service;

string applicationName = "DirMaker";
using var mutex = new Mutex(false, applicationName);

// Single instance of application check
bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
if (isAnotherInstanceOpen)
{
    throw new Exception("Only one instance of the application allowed");
}

// Set exe directory to current directory, important when doing Windows services otherwise runs out of System32
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure logging
Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
        // .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.log", applicationName)))
        .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// CORS
builder.Services.AddCors(options => options.AddPolicy("FrontEnd", pb => pb.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Database connection
builder.Services.AddDbContext<DatabaseContext>(opt => opt.UseSqlite($"Filename={builder.Configuration.GetValue<string>("DatabaseLocation")}"), ServiceLifetime.Transient);

// Crawlers, Builders, Testers registration
builder.Services.AddSingleton<SmartMatchCrawler>();
builder.Services.AddSingleton<ParascriptCrawler>();
builder.Services.AddSingleton<RoyalMailCrawler>();

builder.Services.AddSingleton<SmartMatchBuilder>();
builder.Services.AddSingleton<ParascriptBuilder>();
builder.Services.AddSingleton<RoyalMailBuilder>();

builder.Services.AddSingleton<StatusReporter>();

// Build Application
WebApplication app = builder.Build();

// Database build and validation
DatabaseContext context = app.Services.GetService<DatabaseContext>();
context.Database.EnsureCreated();

// Register Middleware
app.UseCors("FrontEnd");
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Cancellation tokens
Dictionary<string, CancellationTokenSource> cancelTokens = new()
{
    {"SmartMatchCrawler", new()},
    {"ParascriptCrawler", new()},
    {"RoyalMailCrawler", new()},

    {"SmartMatchBuilder", new()},
    {"ParascriptBuilder", new()},
    {"RoyalMailBuilder", new()},
};

// Status
app.MapGet("/status", async (HttpContext context, StatusReporter statusReporter) =>
{
    context.Response.Headers.Add("Content-Type", "text/event-stream");

    for (var i = 0; true; i++)
    {
        string message = statusReporter.Report();
        byte[] bytes = Encoding.ASCII.GetBytes($"data: {message}\r\r");

        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        await context.Response.Body.FlushAsync();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
});

// Crawler Endpoints
app.MapGet("/smartmatch/crawler/{moduleCommand}", (SmartMatchCrawler smartMatchCrawler, string moduleCommand) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["SmartMatchCrawler"] = new();
            Task.Run(() => smartMatchCrawler.Start(cancelTokens["SmartMatchCrawler"].Token));
            return Results.Ok("Started - Normal mode");

        case "autostart":
            cancelTokens["SmartMatchCrawler"] = new();
            Task.Run(() => smartMatchCrawler.AutoStart(cancelTokens["SmartMatchCrawler"].Token));
            return Results.Ok("Started - Auto mode");

        case "stop":
            cancelTokens["SmartMatchCrawler"].Cancel();
            return Results.Ok("Stopped");

        default:
            return Results.BadRequest();
    }
});

app.MapGet("/parascript/crawler/{moduleCommand}", (ParascriptCrawler parascriptCrawler, string moduleCommand) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["ParascriptCrawler"] = new();
            Task.Run(() => parascriptCrawler.Start(cancelTokens["ParascriptCrawler"].Token));
            return Results.Ok("Started - Normal mode");

        case "autostart":
            cancelTokens["ParascriptCrawler"] = new();
            Task.Run(() => parascriptCrawler.AutoStart(cancelTokens["ParascriptCrawler"].Token));
            return Results.Ok("Started - Auto mode");

        case "stop":
            cancelTokens["ParascriptCrawler"].Cancel();
            return Results.Ok("Stopped");

        default:
            return Results.BadRequest();
    }
});

app.MapGet("/royalmail/crawler/{moduleCommand}", (RoyalMailCrawler royalMailCrawler, string moduleCommand) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["RoyalMailCrawler"] = new();
            Task.Run(() => royalMailCrawler.Start(cancelTokens["RoyalMailCrawler"].Token));
            return Results.Ok("Started - Normal mode");

        case "autostart":
            cancelTokens["RoyalMailCrawler"] = new();
            Task.Run(() => royalMailCrawler.AutoStart(cancelTokens["RoyalMailCrawler"].Token));
            return Results.Ok("Started - Auto mode");

        case "stop":
            cancelTokens["RoyalMailCrawler"].Cancel();
            return Results.Ok("Stopped");

        default:
            return Results.BadRequest();
    }
});



// Builder Endpoints
app.MapGet("/smartmatch/builder/{moduleCommand}/{dataYearMonth?}/{cycle?}", (SmartMatchBuilder smartMatchBuilder, string moduleCommand, string cycle, string dataYearMonth) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["SmartMatchBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => smartMatchBuilder.Start(cycle, dataYearMonth, cancelTokens["SmartMatchBuilder"]));
            return Results.Ok("Started - Normal mode");

        case "autostart":
            cancelTokens["SmartMatchBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => smartMatchBuilder.AutoStart(cancelTokens["SmartMatchBuilder"]));
            return Results.Ok("Started - Auto mode");

        case "stop":
            cancelTokens["SmartMatchBuilder"].Cancel();
            Utils.KillSmProcs();
            return Results.Ok("Stopped");

        default:
            return Results.BadRequest();
    }
});

app.MapGet("/parascript/builder/{moduleCommand}/{dataYearMonth?}", (ParascriptBuilder parascriptBuilder, string moduleCommand, string dataYearMonth) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["ParascriptBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => parascriptBuilder.Start(dataYearMonth, cancelTokens["ParascriptBuilder"].Token));
            return Results.Ok("Started - Normal mode");

        case "autostart":
            cancelTokens["ParascriptBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => parascriptBuilder.AutoStart(cancelTokens["ParascriptBuilder"].Token));
            return Results.Ok("Started - Auto mode");

        case "stop":
            cancelTokens["ParascriptBuilder"].Cancel();
            Utils.KillSmProcs();
            return Results.Ok("Stopped");

        default:
            return Results.BadRequest();
    }
});

app.MapGet("/royalmail/builder/{moduleCommand}/{dataYearMonth?}/{key?}", (RoyalMailBuilder royalMailBuilder, string moduleCommand, string dataYearMonth, string key) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["RoyalMailBuilder"] = new();
            Utils.KillRmProcs();
            Task.Run(() => royalMailBuilder.Start(dataYearMonth, key, cancelTokens["RoyalMailBuilder"].Token));
            return Results.Ok("Started - Normal mode");

        // case "autostart":
        //     cancelTokens["RoyalMailBuilder"] = new();
        //     Utils.KillRmProcs();
        //     Task.Run(() => royalMailBuilder.AutoStart(cancelTokens["RoyalMailBuilder"].Token));
        //     return Results.Ok("Started - Auto mode");

        case "stop":
            cancelTokens["RoyalMailBuilder"].Cancel();
            Utils.KillRmProcs();
            return Results.Ok("Stopped");

        default:
            return Results.BadRequest();
    }
});

app.Run();
