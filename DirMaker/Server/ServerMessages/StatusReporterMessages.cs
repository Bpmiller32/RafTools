using DataObjects;

namespace Server.ServerMessages;

public class ModuleReporter
{
    public CrawlerReporter Crawler { get; set; } = new();
    public BuilderReporter Builder { get; set; } = new();
}

public class CrawlerReporter
{
    public ModuleStatus Status { get; set; }
    public int Progress { get; set; }
    public string Message { get; set; } = "";
    public string CurrentTask { get; set; } = "";
    public ReadyToBuildReporter ReadyToBuild { get; set; } = new();
}

public class ReadyToBuildReporter
{
    public string DataYearMonth { get; set; } = "";
    public string FileCount { get; set; } = "";
    public string DownloadDate { get; set; } = "";
    public string DownloadTime { get; set; } = "";
}

public class BuilderReporter
{
    public ModuleStatus Status { get; set; }
    public int Progress { get; set; }
    public string Message { get; set; } = "";
    public string CurrentTask { get; set; } = "";
    public BuildCompleteReporter BuildComplete { get; set; } = new();
}

public class BuildCompleteReporter
{
    public string DataYearMonth { get; set; } = "";
}