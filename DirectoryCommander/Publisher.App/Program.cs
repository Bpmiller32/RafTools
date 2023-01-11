using System.Security.Principal;
using Publisher.App;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

#pragma warning disable CA1416 // ignore that admin check is Windows only 
string applicationName = "Publisher.App";

try
{
    using var mutex = new Mutex(false, applicationName);

    // Configure logger
    Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
            // .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), String.Format(".\\Log\\{0}.txt", applicationName))
            .CreateLogger();

    // Set exe directory to current directory, important when doing Windows services otherwise runs out of System32
    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

    // Single instance of application check
    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
    if (isAnotherInstanceOpen)
    {
        throw new Exception("Only one instance of the application allowed");
    }

    // Check for admin, error if admin isn't present
    WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
    bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
    if (!isElevated)
    {
        throw new Exception("Application does not have administrator privledges");
    }

    // Create custom configuration outside of Generic Host to access value during Generic Host creation
    IConfiguration configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .AddJsonFile("appsettings.json")
        .Build();

    string databaseLocation = configuration.GetValue<string>("settings:DatabaseLocation");

    IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog()
        .ConfigureAppConfiguration(host =>
        {
            host.Sources.Clear();
            host.AddConfiguration(configuration);
        })
        .ConfigureServices(services => services.AddHostedService<Worker>())
        .Build();

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal("There was a problem with the service");
    Log.Fatal(e.Message);
}
finally
{
    Log.CloseAndFlush();
}