using IoMDirectoryBuilder.Common;

namespace IoMDirectoryBuilder.Console;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly IHostApplicationLifetime lifetime;

    private readonly Settings settings;
    private readonly PafBuilder pafBuilder;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime)
    {
        this.logger = logger;
        this.lifetime = lifetime;

        settings = new();
        pafBuilder = new()
        {
            ReportStatus = (text) => logger.LogInformation("{text}", text),
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Perform settings checks first, missing files and/or args are caught and handled separate from potential issues in the main procedure
        try
        {
            settings.CheckArgs();
            settings.CheckPaths();
            settings.CheckMissingPafFiles();
            settings.CheckMissingSmiFiles();
            settings.CheckMissingToolFiles();
        }
        catch (ArgumentException e)
        {
            logger.LogError("{e}", e.Message);
            lifetime.StopApplication();

            PrintUsage();

            return;
        }
        catch (Exception e)
        {
            logger.LogError("{e}", e.Message);
            lifetime.StopApplication();

            return;
        }

        // Main builder procedure
        try
        {
            pafBuilder.StoppingToken = stoppingToken;
            pafBuilder.Settings = settings;

            pafBuilder.Cleanup(clearOutput: true);
            pafBuilder.ConvertMainFile();
            pafBuilder.ConvertPafData();
            pafBuilder.Compile();
            await pafBuilder.Output(deployToAp: bool.Parse(settings.DeployToAp));
            pafBuilder.Cleanup(clearOutput: false);

            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("** Directory build complete **");
            }
        }
        catch (InvalidOperationException) { /* To catch and do nothing on exception thrown by Process when a cancel happens before Process.Start() */ }
        catch (Exception e)
        {
            logger.LogError("{e}", e.Message);
        }
        finally
        {
            Utils.KillRmProcs();
            lifetime.StopApplication();
        }
    }

    private void PrintUsage()
    {
        const string usage = "\nIoMDirectoryBuilder v1.1\n\nIoMDirectoryBuilder converts Royal Mail PAF data files into an SMi directory.\n\n\nUsage: IoMDirectoryBuilder.exe --[Parameters] [Arguments]\n\nExample: IoMDirectoryBuilder.exe --PafFilesPath \"C:\\January PAF data\" --SmiFilesPath \"C:\\SMiBuildFiles\" --DeployToAp \"true\"\n\nRequired Parameters:\n--PafFilesPath [arg]    Directory containing PAF data files\n--SmiFilesPath [arg]    Directory containing SMi build files and tools\n\nOptional Parameters:\n--DeployToAp [arg]      Option to install the created SMi directory to Argosy Post.\n                        This parameter requires the application to have elevated privledges.\n                        Argument must be \"true\" or \"false\" in quotes. Default value is \"false\".\n";

        System.Console.WriteLine(usage);
    }
}
