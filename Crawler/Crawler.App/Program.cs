using System.Net;
using Crawler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Crawler.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Crucially important for Windows Service, otherwise working directory runs out of Windows\System32
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
                .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), @".\LogFile.txt")
                .WriteTo.DiscordSink()
                .CreateLogger();
            try
            {
                Log.Information("Starting up the services");
                CreateHostBuilder(args).Build().Run();
                return;
            }
            catch (System.Exception e)
            {
                Log.Fatal("There was a problem with a service");
                Log.Fatal(e.Message);
                return;
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
                    // services.AddHostedService<SmartmatchCrawler>();
                    // services.AddHostedService<ParascriptCrawler>();
                    services.AddHostedService<RoyalCrawler>();
                    services.AddDbContext<DatabaseContext>(opt =>
                    {
                        opt.UseSqlite(@"Filename=.\DirectoryCollection.db");
                    });
                });
    }
}
