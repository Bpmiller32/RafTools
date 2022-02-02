using System;
using System.IO;
using System.Security.Principal;
using System.Threading;
using Crawler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

#pragma warning disable CA1416 // ignore that calls to manipulate services and check for admin are Windows only 

namespace Crawler.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var mutex = new Mutex(false, "DirCrawler"))
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("System", LogEventLevel.Warning)
                        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
                        .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), @".\Log\CrawlerLog.txt")
                        .WriteTo.DiscordSink()
                        .CreateLogger();

                    // Crucially important for Windows Service, otherwise working directory runs out of Windows\System32
                    System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                    // Not needed, Serilog can punch through and create folders
                    // Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), @"Log"));

                    // Single instance of application check
                    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
                    if (isAnotherInstanceOpen)
                    {
                        throw new Exception("Only one instance of the application allowed");
                    }

                    // Attach method to application closing event handler to kill all spawned subprocess. Put it after singleton check in case another instance is open
                    // AppDomain.CurrentDomain.ProcessExit += Utils.KillAllProcs;

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

                    CreateHostBuilder(args).Build().Run();
                }
            }
            catch (System.Exception e)
            {
                Log.Fatal("There was a problem with a service");
                Log.Fatal(e.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<SmartmatchCrawler>();
                    services.AddHostedService<ParascriptCrawler>();
                    services.AddHostedService<RoyalCrawler>();
                    services.AddDbContext<DatabaseContext>(opt =>
                    {
                        opt.UseSqlite(@"Filename=.\DirectoryCollection.db");
                    });
                });
    }
}
