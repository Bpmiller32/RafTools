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
            // Weird constructor I know, for code reuse with gui version. ReportProgress does nothing in console version
            ReportStatus = (text) => logger.LogInformation("{text}", text),
            // ReportProgress = (_, __) => { }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Perform settings checks separately, missing files and/or args are caught and handled separate from potential issues in the main procedure
        try
        {
            // Debug
            // settings.PafFilesPath = @"C:\Users\billy\Desktop\Y22M09";
            // settings.SmiFilesPath = @"C:\Users\billy\Documents\GitHub\RafTools\IsleBuilder\SMiBuildFiles";
            // settings.WorkingPath = Path.Combine(settings.SmiFilesPath, "Temp");
            // settings.OutputPath = Path.Combine(settings.SmiFilesPath, "Output");

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
        const string usage = "IoMDirectoryBuilder v1.0\n\nIoMDirectoryBuilder converts Royal Mail PAF data files into an SMi directory.\n\nUsage: IoMDirectoryBuilder.exe --[Parameters] [Arguments]\nExample: IoMDirectoryBuilder.exe --PafFilesPath \"C:\\January PAF data\" --SmiFilesPath C:\\SMiBuildFiles --DeployToAp \"true\"\n\nRequired Parameters:\n--PafFilesPath [arg]    Directory containing PAF data files\n--SmiFilesPath [arg]    Directory containing SMi build files and tools\n\nOptional Parameters:\n--DeployToAp [arg]      Option to install the created SMi directory to Argosy Post.\n                        This parameter requires the application to have elevated privledges.\n                        Argument must be \"true\" or \"false\" in quotes. Default value is \"false\".";

        logger.LogInformation("{usage}", usage);
    }
}
