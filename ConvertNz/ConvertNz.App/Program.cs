using System.Reflection;
using ConvertNz.App;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

string applicationName = "ConvertNz";
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

    // Check args
    if (string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
    {
        Log.Information("usage: ConvertNz.exe <Location of addressDb.csv> <Location to write addressDbConverted.csv>");
    }

    // Remove previous converted file if exists
    File.Delete(args[1]);
}
catch (Exception e)
{
    Log.Error(e.Message);
    Environment.Exit(1);
}

// Main logic
NzLine nzLine = new();
PropertyInfo[] nzProperties = nzLine.GetType().GetProperties();
int count = 0;
string line;

try
{
    using StreamReader sr = new(args[0]);
    using StreamWriter sw = new(args[1]);

    // Write header line
    line = sr.ReadLine();
    sw.WriteLine(line);

    // Read addressDb line by line
    while ((line = sr.ReadLine()) != null)
    {
        // Split fields and assign to object for easy organization
        string[] splitLine = line.Split(',');
        for (int i = 0; i < splitLine.Length; i++)
        {
            nzProperties[i].SetValue(nzLine, splitLine[i]);
        }

        // Separate HouseNumber into custom fields
        if (nzLine.HOUSENUMBER.Contains('/'))
        {
            string[] splitHouseNumber = nzLine.HOUSENUMBER.Split('/');
            nzLine.CUSTOM2 = splitHouseNumber[0];
            nzLine.CUSTOM3 = splitHouseNumber[1];
        }

        // Write converted output
        for (int i = 0; i < nzProperties.Length; i++)
        {
            if (i == nzProperties.Length - 1)
            {
                sw.Write(nzProperties[i].GetValue(nzLine) + "\n");
                break;
            }

            sw.Write(nzProperties[i].GetValue(nzLine) + ",");
        }

        // Log status of convert
        count++;
        if (count % 50000 == 0)
        {
            Log.Information("Addresses processed: {0}", count);
        }
    }

    Log.Information("*** DONE ***");
}
catch (Exception e)
{
    Log.Error(e.Message);
    Environment.Exit(1);
}