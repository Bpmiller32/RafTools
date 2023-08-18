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
using Server.Tester;

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
        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u1}] {@m}\n{@x}"))
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

builder.Services.AddSingleton<DirTester>();

builder.Services.AddSingleton<StatusReporter>();
builder.Services.AddSingleton<SynchronizeDb>();

// Build Application
WebApplication app = builder.Build();

// Database build and validation
DatabaseContext context = app.Services.GetService<DatabaseContext>();
context.Database.EnsureCreated();

// Register server address
IConfiguration config = app.Services.GetService<IConfiguration>();
string serverAddress = config.GetValue<string>("ServerAddress");
app.Urls.Add("http://localhost:5000");
app.Urls.Add(serverAddress);


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
        string message = statusReporter.UpdateReport();
        byte[] bytes = Encoding.ASCII.GetBytes($"data: {message}\r\r");

        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        await context.Response.Body.FlushAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
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
            return Results.Ok();

        case "autostart":
            cancelTokens["SmartMatchCrawler"] = new();
            Task.Run(() => smartMatchCrawler.AutoStart(cancelTokens["SmartMatchCrawler"].Token));
            return Results.Ok();

        case "stop":
            cancelTokens["SmartMatchCrawler"].Cancel();
            return Results.Ok();

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
            return Results.Ok();

        case "autostart":
            cancelTokens["ParascriptCrawler"] = new();
            Task.Run(() => parascriptCrawler.AutoStart(cancelTokens["ParascriptCrawler"].Token));
            return Results.Ok();

        case "stop":
            cancelTokens["ParascriptCrawler"].Cancel();
            return Results.Ok();

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
            return Results.Ok();

        case "autostart":
            cancelTokens["RoyalMailCrawler"] = new();
            Task.Run(() => royalMailCrawler.AutoStart(cancelTokens["RoyalMailCrawler"].Token));
            return Results.Ok();

        case "stop":
            cancelTokens["RoyalMailCrawler"].Cancel();
            return Results.Ok();

        default:
            return Results.BadRequest();
    }
});

// Builder Endpoints
app.MapGet("/smartmatch/builder/{moduleCommand}/{dataYearMonth?}/{cycle?}/{expireDays?}", (SmartMatchBuilder smartMatchBuilder, string moduleCommand, string cycle, string dataYearMonth, string expireDays) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["SmartMatchBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => smartMatchBuilder.Start(cycle, dataYearMonth, cancelTokens["SmartMatchBuilder"]));
            return Results.Ok();

        case "autostart":
            cancelTokens["SmartMatchBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => smartMatchBuilder.AutoStart(cancelTokens["SmartMatchBuilder"]));
            return Results.Ok();

        case "customstart":
            cancelTokens["SmartMatchBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => smartMatchBuilder.Start(cycle, dataYearMonth, cancelTokens["SmartMatchBuilder"], expireDays));
            return Results.Ok();

        case "stop":
            cancelTokens["SmartMatchBuilder"].Cancel();
            Utils.KillSmProcs();
            return Results.Ok();

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
            return Results.Ok();

        case "autostart":
            cancelTokens["ParascriptBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => parascriptBuilder.AutoStart(cancelTokens["ParascriptBuilder"].Token));
            return Results.Ok();

        case "stop":
            cancelTokens["ParascriptBuilder"].Cancel();
            Utils.KillSmProcs();
            return Results.Ok();

        default:
            return Results.BadRequest();
    }
});

app.MapGet("/royalmail/builder/{moduleCommand}/{dataYearMonth?}/{key?}/{autoStartTime?}", (RoyalMailBuilder royalMailBuilder, string moduleCommand, string dataYearMonth, string key, string autoStartTime) =>
{
    switch (moduleCommand)
    {
        case "start":
            cancelTokens["RoyalMailBuilder"] = new();
            Utils.KillRmProcs();
            Task.Run(() => royalMailBuilder.Start(dataYearMonth, key, cancelTokens["RoyalMailBuilder"].Token));
            return Results.Ok();

        case "autostart":
            cancelTokens["RoyalMailBuilder"] = new();
            Utils.KillRmProcs();
            Task.Run(() => royalMailBuilder.AutoStart(autoStartTime, cancelTokens["RoyalMailBuilder"].Token));
            return Results.Ok();

        case "stop":
            cancelTokens["RoyalMailBuilder"].Cancel();
            Utils.KillRmProcs();
            return Results.Ok();

        default:
            return Results.BadRequest();
    }
});

// Tester
app.MapGet("/dirtester/{moduleCommand}/{directoryName}", (DirTester dirTester, string moduleCommand, string directoryName) =>
{
    switch (moduleCommand)
    {
        case "Start":
            Task.Run(() => dirTester.Start(directoryName));
            return Results.Ok();

        default:
            return Results.BadRequest();
    }
});

// Debug
app.MapGet("/debug/{moduleCommand}", (SynchronizeDb synchronizeDb, string moduleCommand) =>
{
    switch (moduleCommand)
    {
        case "ScanDb":
            synchronizeDb.ScanDb();
            return Results.Ok("Scanning Db, adding/removing to Db accordingly");

        case "ScanFilesystem":
            synchronizeDb.ScanFilesystem();
            return Results.Ok("Scanning filesystem, adding to Db accordingly");

        default:
            return Results.BadRequest();
    }
});

app.Run();
