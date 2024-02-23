using System.Text.Json;

public class StatusReporter
{
    private readonly Dictionary<string, BaseModule> modules = new();

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
        // Construct JSON object to send to client
        var jsonObject = new
        {
            SmartMatch = new
            {
                Crawler = new
                {
                    modules["smartMatchCrawler"].Status,
                    modules["smartMatchCrawler"].Progress,
                    modules["smartMatchCrawler"].Message,
                    ReadyToBuild = new
                    {
                        DataYearMonth = "202312|202401|202402",
                        FileCount = "4|4|4",
                        DownloadDate = "2/14/2024|2/15/2024|2/16/2024",
                        DownloadTime = "3:45 pm|4:45 pm|5:45 pm",
                    }
                },
            },
        };

        return JsonSerializer.Serialize(jsonObject);
    }
}
