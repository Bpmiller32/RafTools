using System.Text.Json;

public class StatusReporter
{
    private readonly Dictionary<string, BaseModule> modules = new();
    private readonly Dictionary<string, string> dbBuilds = new();

    public StatusReporter()
    {
        modules.Add("smartMatchCrawler", new BaseModule()
        {
            Status = ModuleStatus.Ready,
        });
        modules.Add("smartMatchBuilder", new BaseModule()
        {
            Status = ModuleStatus.Ready,
        });

        // dbBuilds.Add("smNReadytoBuild", string.Join("|", "202308|202309"));
        // dbBuilds.Add("smNBuildComplete", string.Join("|", "202308"));
        dbBuilds.Add("smOReadytoBuild", string.Join("|", "202308|202309"));
        dbBuilds.Add("smOBuildComplete", string.Join("|", ""));
    }

    public void ToggleStatus()
    {
        if (modules["smartMatchCrawler"].Status == ModuleStatus.InProgress)
        {
            modules["smartMatchCrawler"].Status = ModuleStatus.Ready;
            return;
        }

        if (modules["smartMatchCrawler"].Status == ModuleStatus.Ready)
        {
            modules["smartMatchCrawler"].Status = ModuleStatus.InProgress;
            return;
        }
    }

    public string UpdateReport()
    {
        // Update db's only if nessasary, otherwise use stored values
        foreach (var module in modules)
        {
            if (!module.Value.SendDbUpdate)
            {
                continue;
            }
            // Turn off the flag
            module.Value.SendDbUpdate = false;
        }

        var jsonObject = new
        {
            SmartMatch = new
            {
                Crawler = new
                {
                    modules["smartMatchCrawler"].Status,
                    modules["smartMatchCrawler"].Progress,
                    modules["smartMatchCrawler"].Message
                },

                // IsReadyForBuildN = dbBuilds["smNReadytoBuild"],
                // IsBuildCompleteN = dbBuilds["smNBuildComplete"],
                IsReadyForBuildO = dbBuilds["smOReadytoBuild"],
                IsBuildCompleteO = dbBuilds["smOBuildComplete"]
            },
        };

        return JsonSerializer.Serialize(jsonObject);
    }
}
