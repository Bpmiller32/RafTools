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
    private Dictionary<string, bool> errors = new Dictionary<string, bool>() {{@"SmartMatch", false}, {@"Parascript", false}, {@"RoyalMail", false}};
    private Settings smSettings;
    private Settings psSettings;
    private Settings rmSettings;

    public BuildManager(ILogger<BuildManager> logger, IOptionsMonitor<Settings> settings)
    {
        this.logger = logger;
        this.smSettings = settings.Get(Settings.SmartMatch);
        this.psSettings = settings.Get(Settings.Parascript);
        this.rmSettings = settings.Get(Settings.RoyalMail);
    }

    public void RunTask(string data)
    {
        if (data.Contains("SmartMatch") && SmBuild.Status == BuildStatus.Ready)
        {
            logger.LogInformation("Starting builder: SmartMatch");
            SmBuild.Status = BuildStatus.InProgress;

            SmBuild.Task = Task.Run(async () =>
            {
                try
                {
                    Settings.Validate(smSettings);
                    SmBuilder sm = new SmBuilder(smSettings.AddressDataPath, smSettings.OutputPath, smSettings.DataMonth, smSettings.DataYear, smSettings.User, smSettings.Pass);

                    sm.Cleanup();
                    await sm.Build();

                    SmBuild.Status = BuildStatus.Ready;                    
                }
                catch (System.Exception e)
                {
                    logger.LogError("SmartMatch: " + e.Message);
                    SmBuild.Status = BuildStatus.Error;          
                }
            });
        }

        if (data.Contains("Parascript") && PsBuild.Status == BuildStatus.Ready)
        {
            logger.LogInformation("Starting builder: Parascript");
            System.Console.WriteLine(DateTime.Now);
            PsBuild.Status = BuildStatus.InProgress;

            PsBuild.Task = Task.Run(async () =>
            {
                try
                {                    
                    Settings.Validate(psSettings);
                    ParaBuilder ps = new ParaBuilder(psSettings.AddressDataPath, psSettings.WorkingPath, psSettings.OutputPath);

                    ps.CheckInput();
                    ps.Cleanup();
                    ps.FindDate();
                    await ps.Extract();
                    await ps.Archive();
                
                    PsBuild.Status = BuildStatus.Ready;
                }
                catch (System.Exception e)
                {
                    logger.LogError("Parascript: " + e.Message);
                    PsBuild.Status = BuildStatus.Error;
                }
            });
        }

        if (data.Contains("RoyalMail") && RmBuild.Status == BuildStatus.Ready)
        {
            logger.LogInformation("Starting builder: RoyalMail");
            RmBuild.Status = BuildStatus.InProgress;

            RmBuild.Task = Task.Run(async () =>
            {
                try
                {
                    Settings.Validate(rmSettings);
                    RoyalBuilder rm = new RoyalBuilder(rmSettings.AddressDataPath, rmSettings.WorkingPath, rmSettings.OutputPath, rmSettings.Key);
                    
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
                    errors["RoyalMail"] = true;          
                }
            });
        }
    }  
}