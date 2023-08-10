namespace Server.Common;

public class DirModule
{
    public ModuleStatus Status { get; set; }
    public int Progress { get; set; }
    public string Message { get; set; }

    public SettingsValidator Settings { get; set; } = new();

    // Helper methods
    public async Task WaitForDownload(ILogger logger, DatabaseContext context, BaseFile file, string fileType, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Download in progress was stopped due to cancellation");
            return;
        }

        string path = Path.Combine(Settings.AddressDataPath, file.DataYearMonth);
        string[] files = Directory.GetFiles(path, "*.CRDOWNLOAD");

        if (files.Length < 1)
        {
            logger.LogDebug("Finished downloading");
            file.OnDisk = true;
            file.DateDownloaded = DateTime.Now;

            if (fileType == "SmartMatch")
            {
                context.UspsFiles.Update((UspsFile)file);
            }
            if (fileType == "Parascript")
            {
                context.ParaFiles.Update((ParaFile)file);
            }

            context.SaveChanges();
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        await WaitForDownload(logger, context, file, fileType, stoppingToken);
    }

    public static void Cleanup(string path, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Cleanup from previous run
        DirectoryInfo cleanupPath = new(path);

        foreach (var file in cleanupPath.GetFiles())
        {
            file.Delete();
        }
        foreach (var dir in cleanupPath.GetDirectories())
        {
            dir.Delete(true);
        }
    }
}
