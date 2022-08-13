using DafsClrHelper;

namespace BillyService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        bool final = DafsFunctions.GetBoolValueFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Final");
        string postcode = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Postcode");
        // DafsFunctions.SaveAsTiff(@"C:\Users\billy\Desktop\test.dr", "", @"C:\Users\billy\Desktop\testing.tif");
        string xml = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Directory XML");

        string test1 = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "Injected Filename");
        string test2 = DafsFunctions.GetWideStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "Injected Filename");

        long los = DafsFunctions.GetLongValueFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Level of Sort");

        string apVersion = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "Settings", "Argosy Version");

        long elapsedTime = DafsFunctions.GetLongValueFromFile(@"C:\Users\billy\Desktop\test.dr", "Rotation", "Elapsed Time");
        // byte[] imgBytes = DafsFunctions.GetTiffImage(@"C:\Users\billy\Desktop\test.dr");

        // var ms = new MemoryStream(imgBytes);
        // var test = Image.FromStream(ms);

        System.Console.WriteLine("Final: {0}", final);
        System.Console.WriteLine("Postcode: {0}", postcode);
        await Task.Delay(1000, stoppingToken);
    }
}
