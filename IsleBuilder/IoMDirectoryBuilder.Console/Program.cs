using IoMDirectoryBuilder.Common;
using IoMDirectoryBuilder.Console;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

string applicationName = "IoMDirectoryBuilder";
IHost host;

try
{
    using var mutex = new Mutex(false, applicationName);

    // Configure logger
    Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss}]  [{@l:u3}] {@m}\n{@x}"))
            .CreateLogger();

    // Set exe directory to current directory, important when doing Windows services otherwise runs out of System32
    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

    // Single instance of application check
    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
    if (isAnotherInstanceOpen)
    {
        throw new Exception("Another instance of this application is already running");
    }

    // Attach method to application closing event handler to kill all spawned subprocess. Placed after singleton check in case another instance is open
    AppDomain.CurrentDomain.ProcessExit += Utils.KillAllProcs;

    // Create dotnet core host
    host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices(services => services.AddHostedService<Worker>())
        .Build();

    // Establish an event handler to process key press events.
    Console.CancelKeyPress += CancelHandler;

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal("There was a problem with the application");
    Log.Fatal(e.Message);
}
finally
{
    Log.CloseAndFlush();
}

void CancelHandler(object sender, ConsoleCancelEventArgs args)
{
    if (!Utils.CancelRequested)
    {
        Log.Warning("Closing application and spawned process (may take a few seconds to cleanly shutdown)....");
        Utils.CancelRequested = true;

        host.StopAsync();
        Utils.KillRmProcs();
    }
}
