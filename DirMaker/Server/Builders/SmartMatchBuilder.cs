using Com.Raf.Utility;
using Com.Raf.Xtl.Build;
using Server.Common;

namespace Server.Builders;

public class SmartMatchBuilder : BaseModule
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
                    await Task.Delay(TimeSpan.FromSeconds(2));
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
            Message = "Check logs for more details";
            logger.LogError($"{e.Message}");
        }
    }

    public async Task Start(string cycle, string dataYearMonth, CancellationTokenSource stoppingTokenSource)
    {
        logger.LogInformation("Starting Builder");
        Status = ModuleStatus.InProgress;
        Message = "Starting Builder";
        Progress = 1;

        Settings.Validate(config);

        Task builderTask = Task.CompletedTask;
        string dataSourcePath = Path.Combine(Settings.AddressDataPath, dataYearMonth);

        if (cycle == "N")
        {
            string sourceFolder = Path.Combine(dataSourcePath, $"Cycle-{cycle}");
            string dataOutputPath = Path.Combine(Settings.OutputPath, dataYearMonth, "Cycle-N");

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
            string dataOutputPath = Path.Combine(Settings.OutputPath, dataYearMonth, "Cycle-O");

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
            string dataOutputPath = Path.Combine(Settings.OutputPath, dataYearMonth, "Cycle-N-Using-O");

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
                Message = "";
                Progress = 100;
                logger.LogInformation("XtlBuilder finished running");
                CheckBuildComplete(dataYearMonth, cycle, stoppingTokenSource.Token);
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

        if (status.IndexOf("(was Stage ") != -1)
        {
            int stageNumberIndex = status.IndexOf("(was Stage ");
            int stageNumber = int.Parse(status.Substring(stageNumberIndex + 11, 1));
            Message = $"Completed Stage {stageNumber}";

            switch (stageNumber)
            {
                case 1:
                    Progress = 2;
                    break;
                case 2:
                    Progress = 24;
                    break;
                case 3:
                    Progress = 60;
                    break;
                case 4:
                    Progress = 61;
                    break;
                case 5:
                    Progress = 62;
                    break;
                case 6:
                    Progress = 64;
                    break;
                default:
                    break;
            }
        }

        if (logLevel == Logging.LogLevel.Error)
        {
            cancellationTokenSource.Cancel();
            Status = ModuleStatus.Error;
        }
    }

    private void CheckBuildComplete(string dataYearMonth, string cycle, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        if (cycle == "OtoN")
        {
            cycle = "N";
        }

        // Will be null if Crawler never made a record for it, watch out if running standalone
        UspsBundle bundle = context.UspsBundles.Where(x => dataYearMonth == x.DataYearMonth && cycle == x.Cycle).FirstOrDefault();
        bundle.IsBuildComplete = true;
        bundle.CompileDate = Utils.CalculateDbDate();
        bundle.CompileTime = Utils.CalculateDbTime();

        context.SaveChanges();
        SendDbUpdate = true;
    }
}
