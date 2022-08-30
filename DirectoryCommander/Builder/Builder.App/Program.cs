using System.Security.Principal;
using Common.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

try
{
    using (var mutex = new Mutex(false, "DirBuilder"))
    {
        // Configure logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
            .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), @".\Log\BuilderLog.txt")
            .CreateLogger();

        // Set exe directory to current directory, not needed for this but important when doing Windows services
        System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

        // Single instance of application check
        bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
        if (isAnotherInstanceOpen)
        {
            throw new Exception("Only one instance of the application allowed");
        }

        // Attach method to application closing event handler to kill all spawned subprocess. Put it after singleton check in case another instance is open
        AppDomain.CurrentDomain.ProcessExit += Utils.KillAllProcs;

        // Check for admin
        bool isElevated;
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Error if admin isn't present
        if (!isElevated)
        {
            throw new Exception("Application does not have administrator privledges");
        }

        IHost host = Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<SocketServer>();
                services.AddTransient<SocketConnection>();

                services.AddSingleton<ParaBuilder>();
                services.AddSingleton<RoyalBuilder>();

                services.AddDbContext<DatabaseContext>(opt =>
                {
                    opt.UseSqlite(@"Filename=C:\Users\billy\Documents\GitHub\RafTools\Crawler\Crawler.App\bin\Debug\net6.0\DirectoryCollection.db");
                }, ServiceLifetime.Transient);
            })
            .Build();

        await host.RunAsync();
    }
}
catch (System.Exception e)
{
    Log.Fatal("There was a problem");
    Log.Fatal(e.Message);
}
finally
{
    Log.CloseAndFlush();
}