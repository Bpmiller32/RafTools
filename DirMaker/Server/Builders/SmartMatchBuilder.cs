using Com.Raf.Utility;
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

        Settings.DirectoryName = "SmartMatch";
    }

    public async Task AutoStart(CancellationTokenSource stoppingTokenSource)
    {
        try
        {
            while (!stoppingTokenSource.Token.IsCancellationRequested)
            {
                logger.LogInformation("Starting Builder - Auto mode");

                TimeSpan waitTime = ModuleSettings.CalculateWaitTime(logger, Settings);
                Status = ModuleStatus.Standby;
                await Task.Delay(TimeSpan.FromSeconds(waitTime.TotalSeconds), stoppingTokenSource.Token);

                foreach (UspsBundle bundle in context.UspsBundles.Where(x => x.IsReadyForBuild && !x.IsBuildComplete).ToList())
                {
                    await Start(bundle.Cycle.Substring(bundle.Cycle.Length - 1, 1), bundle.DataYearMonth, stoppingTokenSource);
                }
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug($"{e.Message}");
        }
        catch (Exception e)
        {
            Status = ModuleStatus.Error;
            logger.LogError($"{e.Message}");
        }
    }

    public async Task Start(string cycle, string dataYearMonth, CancellationTokenSource stoppingTokenSource)
    {
        logger.LogInformation("Starting Builder");
        Status = ModuleStatus.InProgress;

        Settings.Validate(config);

        Task builderTask = Task.CompletedTask;
        string dataSourcePath = Path.Combine(Settings.AddressDataPath, dataYearMonth);
        string dataOutputPath = Path.Combine(Settings.OutputPath, dataYearMonth);

        if (cycle == "N")
        {
            string sourceFolder = Path.Combine(dataSourcePath, $"Cycle-{cycle}");

            CycleN2Sha256XtlBuilder smartMatchBuilder = new(dataYearMonth.Substring(2, 4) + "1", sourceFolder, dataOutputPath, Settings.AddressDataPath, "user", "password", "105", "14 15 16 19 20 21 22 23 24 25 26 27 28 29 30 31", "TestFile.Placeholder");
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
            string sourceFolder = Path.Combine(dataSourcePath, $"Cycle-{cycle}");

            CycleOSha256XtlBuilder smartMatchBuilder = new(dataYearMonth.Substring(2, 4) + "1", sourceFolder, dataOutputPath, Settings.AddressDataPath, "user", "password", "105", "14 15 16 19 20 21 22 23 24 25 26 27 28 29 30 31", "TestFile.Placeholder");
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
        else if (cycle == "OtoN")
        {
            string sourceFolder = Path.Combine(dataSourcePath, "Cycle-O");

            CycleN2Sha256XtlBuilder smartMatchBuilder = new(dataYearMonth.Substring(2, 4) + "1", sourceFolder, dataOutputPath, Settings.AddressDataPath, "user", "password", "105", "14 15 16 19 20 21 22 23 24 25 26 27 28 29 30 31", "TestFile.Placeholder");
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

        while (!stoppingTokenSource.Token.IsCancellationRequested)
        {
            if (builderTask.Status == TaskStatus.RanToCompletion)
            {
                logger.LogInformation("XtlBuilder finished running");
                CheckBuildComplete(dataYearMonth, stoppingTokenSource.Token);
                Status = ModuleStatus.Ready;
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Status = ModuleStatus.Error;
    }

    private void UpdateStatus(string status, Logging.LogLevel logLevel)
    {
        logger.LogInformation(status);

        if (logLevel == Logging.LogLevel.Error)
        {
            cancellationTokenSource.Cancel();
            Status = ModuleStatus.Error;
        }
    }

    private void CheckBuildComplete(string dataYearMonth, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Will be null if Crawler never made a record for it, watch out if running standalone
        UspsBundle bundle = context.UspsBundles.Where(x => dataYearMonth == x.DataYearMonth).FirstOrDefault();
        bundle.IsBuildComplete = true;
        bundle.CompileDate = Utils.CalculateDbDate();
        bundle.CompileTime = Utils.CalculateDbTime();

        context.SaveChanges();
    }
}
