using DafsClrHelper;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

string applicationName = "BillyDafs";
using var mutex = new Mutex(false, applicationName);

// Configure logger
Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss}]  [{@l:u3}] {@m}\n{@x}"))
        .CreateLogger();

// Set exe directory to current directory, important when doing Windows services otherwise runs out of System32
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

try
{
    // Single instance of application check
    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
    if (isAnotherInstanceOpen)
    {
        throw new Exception("Only one instance of the application allowed");
    }
}
catch (System.Exception e)
{
    Log.Error(e.Message);
}

Console.WriteLine("Hello, World!");

bool final = DafsFunctions.GetBoolValueFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Final");
string postcode = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Postcode");
string xml = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Directory XML");
string test1 = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "Injected Filename");
string test2 = DafsFunctions.GetWideStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "", "Injected Filename");

long los = DafsFunctions.GetLongValueFromFile(@"C:\Users\billy\Desktop\test.dr", "", "RM: Level of Sort");
string apVersion = DafsFunctions.GetStringPropFromFile(@"C:\Users\billy\Desktop\test.dr", "Settings", "Argosy Version");

Console.WriteLine("Final: {0}", final);
Console.WriteLine("Postcode: {0}", postcode);