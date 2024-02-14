using DataObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly DatabaseContext context;

    public Worker(ILogger<Worker> logger, DatabaseContext context)
    {
        this.logger = logger;
        this.context = context;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ParaBundle newBundle = new()
        {
            DataMonth = file.DataMonth,
            DataYear = file.DataYear,
            DataYearMonth = file.DataYearMonth,
            IsReadyForBuild = false
        };

        // Check if file is unique against the db
        bool fileInDb = context.ParaFiles.Any(x => (file.FileName == x.FileName) && (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

    }
}
