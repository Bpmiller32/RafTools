using Common.Data;

public class ParaTester
{
    public Settings Settings { get; set; } = new Settings { Name = "Parascript" };
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }

    private readonly ILogger<ParaTester> logger;
    private readonly IConfiguration config;
    private readonly SocketConnection connection;

    public ParaTester(ILogger<ParaTester> logger, IConfiguration config, SocketConnection connection)
    {
        this.logger = logger;
        this.config = config;
        this.connection = connection;
    }

    public void ExecuteAsync()
    {
        try
        {
            logger.LogInformation("Starting Tester");
            Status = ComponentStatus.InProgress;

            Settings.Validate(config);

            CheckDisc();

            logger.LogInformation("Test Complete");
            Status = ComponentStatus.Ready;
            connection.SendMessage(DirectoryType.Parascript);
        }
        catch (TaskCanceledException e)
        {
            Status = ComponentStatus.Ready;
            connection.SendMessage(DirectoryType.Parascript);
            logger.LogDebug(e.Message);
        }
        catch (System.Exception e)
        {
            Status = ComponentStatus.Error;
            connection.SendMessage(DirectoryType.Parascript);
            logger.LogError(e.Message);
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

        connection.SendMessage(DirectoryType.Parascript);
    }

    private void CheckDisc()
    {
        // string[] directories = Directory.GetDirectories();
    }

}
