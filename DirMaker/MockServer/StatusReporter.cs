using System.Text.Json;

public class StatusReporter
{
    private readonly Dictionary<string, BaseModule> modules = new();

    private string testDataYearMonth = "202312|202401|202402";
    private string testFileCount = "4|4|4";
    private string testDownloadDate = "2/14/2024|2/15/2024|2/16/2024";
    private string testDownloadTime = "3:45 pm|4:45 pm|5:45 pm";

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

    public void AddDirectory()
    {
        testDataYearMonth += "|202501";
        testFileCount += "|4";
        testDownloadDate += "|10/14/2029";
        testDownloadTime += "|69:69 pm";

    }

    public void ResetDirectory()
    {
        testDataYearMonth = "202312|202401|202402";
        testFileCount = "4|4|4";
        testDownloadDate = "2/14/2024|2/15/2024|2/16/2024";
        testDownloadTime = "3:45 pm|4:45 pm|5:45 pm";
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
                        DataYearMonth = testDataYearMonth,
                        FileCount = testFileCount,
                        DownloadDate = testDownloadDate,
                        DownloadTime = testDownloadTime,
                    }
                },
            },
        };

        return JsonSerializer.Serialize(jsonObject);
    }
}
