using Builder.App.Builders;
using Builder.App.Utils;
using Microsoft.Extensions.Options;

namespace Builder.App;

public class BuildManager
{
    public BuildTask SmBuild { get; set; } = new BuildTask() {Name = "SmartMatch"};
    public BuildTask PsBuild { get; set; } = new BuildTask() {Name = "Parascript"};
    public BuildTask RmBuild { get; set; } = new BuildTask() {Name = "RoyalMail"};
    
    private readonly ILogger<BuildManager> logger;
    private readonly DatabaseContext context;
    private Settings smSettings;
    private Settings psSettings;
    private Settings rmSettings;
    private Action<int> smProgress;
    private Action<int> psProgress;
    private Action<int> rmProgress;

    public BuildManager(ILogger<BuildManager> logger, IServiceScopeFactory factory, IOptionsMonitor<Settings> settings)
    {
        this.logger = logger;
        this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        this.smSettings = settings.Get(Settings.SmartMatch);
        this.psSettings = settings.Get(Settings.Parascript);
        this.rmSettings = settings.Get(Settings.RoyalMail);

        // No SyncronizationContext guards because
        // - SyncronizationContext doesn't exist in .net Core
        // - OutOfBoundsCondition: the BuildManager object will never be disposed before the thread is complete
        // - RaceCondition: There is no 2 resources reading or writing to the BuildTask objects
        // Because of this where the call is performed (also when in that where) isn't breaking
        this.smProgress = SmBuild.ChangeProgress;
        this.psProgress = PsBuild.ChangeProgress;
        this.rmProgress = RmBuild.ChangeProgress;
    }

    public void RunTask(SocketMessage messsage)
    {
        if (messsage.BuildSmartMatch && SmBuild.Status == BuildStatus.Ready)
        {
            logger.LogInformation("Starting builder: SmartMatch");
            SmBuild.Status = BuildStatus.InProgress;
            SmBuild.Progress = 0;

            SmBuild.Task = Task.Run(async () =>
            {
                try
                {
                    Settings.Validate(smSettings, messsage);
                    SmBuilder sm = new SmBuilder(smSettings, context, smProgress);

                    sm.Cleanup();
                    await sm.Build();
                    sm.CheckBuildComplete();

                    SmBuild.Status = BuildStatus.Ready;                    
                }
                catch (System.Exception e)
                {
                    logger.LogError("SmartMatch: " + e.Message);
                    SmBuild.Status = BuildStatus.Error;          
                }
            });
        }

        if (messsage.BuildParascript && PsBuild.Status == BuildStatus.Ready)
        {
            logger.LogInformation("Starting builder: Parascript");
            PsBuild.Status = BuildStatus.InProgress;
            PsBuild.Progress = 0;

            PsBuild.Task = Task.Run(async () =>
            {
                try
                {                    
                    Settings.Validate(psSettings, messsage);
                    ParaBuilder ps = new ParaBuilder(psSettings, context, psProgress);

                    ps.CheckInput();
                    ps.Cleanup(fullClean: true);
                    ps.FindDate();
                    await ps.Extract();
                    await ps.Archive();
                    ps.Cleanup(fullClean: false);
                    ps.CheckBuildComplete();
                
                    PsBuild.Status = BuildStatus.Ready;
                }
                catch (System.Exception e)
                {
                    logger.LogError("Parascript: " + e.Message);
                    PsBuild.Status = BuildStatus.Error;
                }
            });
        }

        if (messsage.BuildRoyalMail && RmBuild.Status == BuildStatus.Ready)
        {
            logger.LogInformation("Starting builder: RoyalMail");
            RmBuild.Status = BuildStatus.InProgress;
            RmBuild.Progress = 0;

            RmBuild.Task = Task.Run(async () =>
            {
                try
                {
                    Settings.Validate(rmSettings, messsage);
                    RoyalBuilder rm = new RoyalBuilder(rmSettings, context, rmProgress);
                    
                    await rm.Extract();
                    rm.Cleanup(fullClean: true);
                    rm.FindDate();
                    rm.UpdateSmiFiles();
                    rm.ConvertPafData();
                    await rm.Compile();
                    await rm.Output();
                    rm.Cleanup(fullClean: false);                    
                }
                catch (System.Exception e)
                {
                    logger.LogError("RoyalMail: " + e.Message);
                    RmBuild.Status = BuildStatus.Error;      
                }
            });
        }
    }  
}