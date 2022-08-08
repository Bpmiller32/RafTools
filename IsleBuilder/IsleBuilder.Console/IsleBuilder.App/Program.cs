using System.Security.Principal;
using IsleBuilder.App;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

#pragma warning disable CA1416 // ignore that calls to manipulate services and check for admin are Windows only 

try
{
    using (var mutex = new Mutex(false, "IsleBuilder"))
    {
        // Configure logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
            .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), @".\IsleBuilder_Log.txt")
            .CreateLogger();

        // Set exe directory to current directory, not needed for this but important when doing Windows services
        System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
        // Attach method to application closing event handler to kill all spawned subprocess. Put it after singleton check in case another instance is open
        AppDomain.CurrentDomain.ProcessExit += Utils.KillProcs;

        // Single instance of application check
        bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
        if (isAnotherInstanceOpen)
        {
            throw new Exception("Only one instance of the application allowed");
        }

        // Check for admin
        bool isElevated = false;
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Warn if admin isn't present
        if (!isElevated)
        {
            Log.Warning("Application does not have administrator privledges");
        }

        IHost host = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.Configure<Settings>(Settings.IoM, context.Configuration.GetSection("settings:IoM"));
                services.AddHostedService<PafBuilder>();
            })
            .Build();

        await host.RunAsync();
    }
}
catch (System.Exception e)
{
    Log.Fatal("There was a problem with the program");
    Log.Fatal(e.Message);
}
finally
{
    System.Console.WriteLine("Press enter to continue...");
    System.Console.ReadLine();
    Log.CloseAndFlush();
}