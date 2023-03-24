using Common.Data;

namespace Tester;

public class ZipTester
{
    public Settings Settings { get; set; } = new Settings { Name = "Zip4" };
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }

    private readonly ILogger<ZipTester> logger;
    private readonly IConfiguration config;

    public ZipTester(ILogger<ZipTester> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }

    public void Execute()
    {
        try
        {
            logger.LogInformation("Starting Tester");
            Status = ComponentStatus.InProgress;
            ChangeProgress(0, reset: true);

            Settings.Validate(config);

            CheckDisc();

            logger.LogInformation("Test Complete");
            Status = ComponentStatus.Ready;
            SocketController.SendMessage();
        }
        catch (Exception e)
        {
            Status = ComponentStatus.Error;
            SocketController.SendMessage();
            logger.LogError("{Message}", e.Message);
        }
    }

    public void ChangeProgress(int changeAmount, bool reset = false)
    {
        if (reset)
        {
            Progress = 0;
        }
        else
        {
            Progress += changeAmount;
        }

        SocketController.SendMessage();
    }

    private void CheckDisc()
    {
        ChangeProgress(1);

        List<string> smFiles = new()
        {
            "Zip4.zip"
        };

        string missingFiles = "";

        foreach (string file in smFiles)
        {
            if (!File.Exists(Path.Combine(Settings.DiscDrivePath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception("Missing files (may have disc in wrong drive): " + missingFiles);
        }

        ChangeProgress(10);
    }
}