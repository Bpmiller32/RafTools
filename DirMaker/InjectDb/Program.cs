using DataObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

try
{
    if (string.IsNullOrEmpty(Environment.GetCommandLineArgs()[1]))
    {
        throw new Exception();
    }
}
catch (Exception)
{
    Console.WriteLine("Usage: InjectDb.exe <database path> <directory> <dataYearMonth>");
    Console.WriteLine("Example: InjectDb.exe C:\\DirectoryCollection.db smartmatch 202402");
    return;
}

// URL to which you want to send the POST request
string url = Environment.GetCommandLineArgs()[1];

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<DatabaseContext>(opt => opt.UseSqlite($"Filename={url}"), ServiceLifetime.Transient);
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();