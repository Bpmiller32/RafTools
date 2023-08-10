using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Server.Builders;
using Server.Common;
using Server.Crawlers;

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

// SmartMatch endpoints
// Crawler Endpoints
app.MapGet("/smartmatch/crawler/{moduleCommand}", (SmartMatchCrawler smartMatchCrawler, [FromRoute] ModuleCommand moduleCommand) =>
{
    if (moduleCommand == ModuleCommand.Start)
    {
        if (smartMatchCrawler.Status == ModuleStatus.InProgress)
        {
            return Results.Conflict("Already started");
        }

        cancelTokens["SmartMatchCrawler"] = new();
        Task.Run(() => smartMatchCrawler.Start(cancelTokens["SmartMatchCrawler"].Token));
        return Results.Ok("Started");
    }
    if (moduleCommand == ModuleCommand.Stop)
    {
        cancelTokens["SmartMatchCrawler"].Cancel();
        return Results.Ok("Stopped");
    }

    return Results.BadRequest();
});

// Builder Endpoints
app.MapGet("/smartmatch/builder/{moduleCommand}/{cycle?}/{dataYearMonth?}", (SmartMatchBuilder smartMatchBuilder, ModuleCommand moduleCommand, string cycle, string dataYearMonth) =>
{
    if (moduleCommand == ModuleCommand.Start && !string.IsNullOrEmpty(cycle) && !string.IsNullOrEmpty(dataYearMonth))
    {
        if (smartMatchBuilder.Status == ModuleStatus.InProgress)
        {
            return Results.Conflict("Already started");
        }

        if (cycle == "N" || cycle == "O")
        {
            cancelTokens["SmartMatchBuilder"] = new();
            Utils.KillSmProcs();
            Task.Run(() => smartMatchBuilder.Start(cycle, dataYearMonth, cancelTokens["SmartMatchBuilder"]));
            return Results.Ok("Started");
        }
    }
    if (moduleCommand == ModuleCommand.Stop)
    {
        cancelTokens["SmartMatchBuilder"].Cancel();
        Utils.KillSmProcs();
        return Results.Ok("Stopped");
    }

    return Results.BadRequest();
});

app.Run();
