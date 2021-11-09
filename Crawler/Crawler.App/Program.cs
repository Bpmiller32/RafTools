using Crawler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Crawler.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .WriteTo.Console(LogEventLevel.Information)
                .WriteTo.File(@".\LogFile.txt", LogEventLevel.Information, "{Timestamp:MM-dd-yyyy HH:mm:ss} [{Level:u3}] {Message}{NewLine}")
                .CreateLogger();

            try
            {
                Log.Information("Starting up the service");
                CreateHostBuilder(args).Build().Run();
                return;
            }
            catch (System.Exception)
            {
                Log.Fatal("There was a problem starting the service");
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
                    services.AddHostedService<Worker>();
                    services.AddDbContext<DatabaseContext>(opt =>
                    {
                        opt.UseSqlite(@"Filename=.\DirectoryCollection.db");
                        opt.EnableSensitiveDataLogging();
                    });
                });
    }
}
