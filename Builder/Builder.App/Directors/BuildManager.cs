using Builder.App.Utils;
using Microsoft.Extensions.Options;

namespace Builder.App.Builders;

public class BuildManager
{
    public Dictionary<string, Task> TaskList { get; set; } = new Dictionary<string, Task>();
    
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
        if (data.Contains("PING"))
        {
            StatusPong(TaskList);
        }

        if (data.Contains("SmartMatch") && !TaskList.ContainsKey("SmartMatch") && !errors["SmartMatch"])
        {
            logger.LogInformation("Starting builder: SmartMatch");

            TaskList.Add("SmartMatch", Task.Run(async () =>
            {
                try
                {
                    Settings.Validate(smSettings);
                    SmBuilder sm = new SmBuilder(smSettings.AddressDataPath, smSettings.OutputPath, smSettings.DataMonth, smSettings.DataYear, smSettings.User, smSettings.Pass);

                    sm.Cleanup();
                    await sm.Build();                    
                }
                catch (System.Exception e)
                {
                    logger.LogError("SmartMatch: " + e.Message);          
                    errors["SmartMatch"] = true;          
                }
            }));
        }

        if (data.Contains("Parascript") && !TaskList.ContainsKey("Parascript") && !errors["Parascript"])
        {
            logger.LogInformation("Starting builder: Parascript");

            TaskList.Add("Parascript", Task.Run(async () =>
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
                }
                catch (System.Exception e)
                {
                    logger.LogError("Parascript: " + e.Message);
                    errors["Parascript"] = true;          
                }
            }));
        }

        if (data.Contains("RoyalMail") && !TaskList.ContainsKey("RoyalMail") && !errors["RoyalMail"])
        {
            logger.LogInformation("Starting builder: RoyalMail");

            TaskList.Add("RoyalMail", Task.Run(async () =>
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
            }));
        }
    }

    public void StatusPong(Dictionary<string, Task> TaskList)
    {
        List<string> tasks = new List<string>() {@"SmartMatch", @"Parascript", @"RoyalMail"};
    
        foreach (string task in tasks)
        {
            // Check if TaskList has the key
            if (!TaskList.ContainsKey(task))
            {
                logger.LogInformation("Builder not running: " + task);
                continue;
            }

            // Otherwise it contains the task, check if the task is running (success or fail because of the try/catch it will always RanToCompletion)
            if (TaskList[task].Status == TaskStatus.RanToCompletion)
            {
                if (errors[task] == true)
                {
                    logger.LogError("Builder error, check log: " + task);
                    continue;
                }

                TaskList.Remove(task);
                logger.LogInformation("Builder not running: " + task);
                continue;
            }

            logger.LogInformation("Builder running: " + task);            
        }
    }
}