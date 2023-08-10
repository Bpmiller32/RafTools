using Com.Raf.Xtl.Build;
using Server.Common;

namespace Server.Builders;

public class SmartMatchBuilder : DirModule
{
    private readonly ILogger<SmartMatchBuilder> logger;
    private readonly IConfiguration config;
    private readonly DatabaseContext context;

    private CancellationTokenSource cancellationTokenSource;

    public SmartMatchBuilder(ILogger<SmartMatchBuilder> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.context = context;

        Settings.Directory = "SmartMatch";
    }

    public async Task Start(string cycle, string dataYearMonth, CancellationTokenSource stoppingTokenSource)
    {
        Settings.Validate(config);

        string sourceFolder = Path.Combine(Settings.AddressDataPath, dataYearMonth, "Cycle-" + cycle);
        string outputFolder = Path.Combine(@"C:\Users\billy\Desktop", dataYearMonth, "Cycle-" + cycle);

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        logger.LogInformation("Starting Builder");
        Status = ModuleStatus.InProgress;
        Task builderTask = Task.CompletedTask;

        if (cycle == "N")
        {
            CycleN2Sha256XtlBuilder smartMatchBuilder = new(dataYearMonth.Substring(2, 4) + "1", sourceFolder, outputFolder, Settings.AddressDataPath, "billy", "password", "105", "14 15 16 19 20 21 22 23 24 25 26 27 28 29 30 31", "TestFile.Placeholder");
            smartMatchBuilder.UpdateStatus += UpdateStatus;
            cancellationTokenSource = stoppingTokenSource;
            builderTask = Task.Run(() =>
            {
                using (stoppingTokenSource.Token.Register(Thread.CurrentThread.Interrupt))
                {
                    smartMatchBuilder.Build(false, false);
                }
            });
        }
        else if (cycle == "O")
        {
        }

        while (!stoppingTokenSource.Token.IsCancellationRequested)
        {
            if (builderTask.Status == TaskStatus.RanToCompletion)
            {
                logger.LogInformation("XtlBuilder ran to completion....");
                Status = ModuleStatus.Ready;
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Status = ModuleStatus.Error;
    }

    private void UpdateStatus(string status)
    {
        logger.LogInformation(status);

        if (status.Contains("Exception"))
        {
            cancellationTokenSource.Cancel();
        }
    }
}
