using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

#pragma warning disable CA1416 // ignore that my calls to manipulate services and check for admin are Windows only 

namespace IsleBuilder.App;

public class PafBuilder : BackgroundService
{
    private readonly ILogger<PafBuilder> logger;
    private readonly IHostApplicationLifetime lifetime;
    private readonly Settings settings;
    private bool isElevated;

    public PafBuilder(ILogger<PafBuilder> logger, IHostApplicationLifetime lifetime, IOptionsMonitor<Settings> settings)
    {
        this.logger = logger;
        this.lifetime = lifetime;
        this.settings = settings.Get(Settings.IoM);
    }
    
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Isle of Man directory builder v0.1");

        // Attach method to application closing event handler to kill all spawned subprocess. Put it after singleton check in case another instance is open
        AppDomain.CurrentDomain.ProcessExit += Utils.KillProcs;

        // Check for admin
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Warn if admin isn't present
        if (!isElevated)
        {
            logger.LogWarning("Application does not have administrator privledges");
        }

        // Warn if moving compiled files to AP is off
        if (!settings.MoveToAp)
        {
            logger.LogWarning("Application set to not move compiled directory into Argosy Post Sync folder");
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        System.Console.WriteLine("\nPlease make sure settings are correct in appsettings.json and folders contain appropriate files\n");
        System.Console.WriteLine("Press enter to continue...");
        System.Console.ReadLine();

        try
        {
            Settings.Validate(settings);

            Cleanup(fullClean: true);
            EncryptSmiFiles();
            ConvertPafData();
            await Compile();
            await Output();
            Cleanup(fullClean: false);

            if (settings.MoveToAp)
            {
                DeployToAp();
            }

            logger.LogInformation("All done!");
        }
        catch (System.Exception e)
        {
            logger.LogError(e.Message);
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private void Cleanup(bool fullClean)
    {
        logger.LogInformation("Cleaning up from previous runs");

        // Kill process that may be running in the background from previous runs
        foreach (Process process in Process.GetProcessesByName("ConvertPafData"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("DirectoryDataCompiler"))
        {
            process.Kill(true);
        }

        // Ensure working and output directories are created and clear them if they already exist
        Directory.CreateDirectory(settings.WorkingPath);
        Directory.CreateDirectory(settings.OutputPath);

        DirectoryInfo wp = new DirectoryInfo(settings.WorkingPath);
        DirectoryInfo op = new DirectoryInfo(settings.OutputPath);

        if (!fullClean)
        {
            foreach (FileInfo file in wp.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in wp.GetDirectories())
            {
                dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                dir.Delete(true);
            }

            Directory.Delete(settings.WorkingPath, true);

            return;    
        }

        foreach (FileInfo file in op.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in op.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    private void EncryptSmiFiles()
    {
        logger.LogInformation("Encrypting dongle licensing and pattern files");

        // Copy SMi build files to working folder "Smi"
        Utils.CopyFiles(settings.BuildFilesPath, Path.Combine(settings.WorkingPath, "Smi"));

        // Encrypt new Uk dongle list, but first wrap the combined paths in quotes to get around spaced directories
        string encryptRepFileName = Utils.WrapQuotes(Path.Combine(settings.BuildToolsPath, "EncryptREP.exe"));
        string encryptRepArgs = @"-x lcs " + Utils.WrapQuotes(Path.Combine(settings.WorkingPath, "Smi", "UK_RM_CM.txt"));

        Process encryptRep = Utils.RunProc(encryptRepFileName, encryptRepArgs);
        encryptRep.WaitForExit();

        // Encrypt patterns, but first wrap the combined paths in quotes to get around spaced directories
        string encryptPatternsFileName = Utils.WrapQuotes(Path.Combine(settings.BuildToolsPath, "EncryptPatterns.exe"));
        string encryptPatternsArgs = @"--patterns " + Utils.WrapQuotes(Path.Combine(settings.WorkingPath, "Smi", "UK_RM_CM_Patterns.xml")) + @" --clickCharge";
        
        Process encryptPatterns = Utils.RunProc(encryptPatternsFileName, encryptPatternsArgs);
        encryptPatterns.WaitForExit();

        // If this file wasn't created then EncryptPatterns silently failed, likeliest cause is missing a redistributable
        if (!File.Exists(Path.Combine(settings.WorkingPath, "Smi", "UK_RM_CM_Patterns.exml")))
        {
            throw new Exception("Missing C++ 2010 x86 redistributable, EncryptPatterns and DirectoryDataCompiler 1.9 won't work. Also check that SQL CE is installed for 1.9");
        }
    }

    private void ConvertPafData()
    {
        logger.LogInformation("Converting PAF data");

        // Move address data files to working folder "Db"
        Utils.CopyFiles(Path.Combine(settings.AddressDataPath, "PAF COMPRESSED STD"), Path.Combine(settings.WorkingPath, "Db"));
        Utils.CopyFiles(Path.Combine(settings.AddressDataPath, "ALIAS"), Path.Combine(settings.WorkingPath, "Db"));

        // Start ConvertPafData tool, listen for output
        string convertPafDataFileName = Utils.WrapQuotes(Path.Combine(settings.BuildToolsPath, "ConvertPafData.exe"));
        string convertPafDataArgs = @"--pafPath " + Utils.WrapQuotes(Path.Combine(settings.WorkingPath, "Db")) + @" --lastPafFileNum 15";

        Process convertPafData = Utils.RunProc(convertPafDataFileName, convertPafDataArgs);
        using (StreamReader sr = convertPafData.StandardOutput)
        {
            string line;
            Regex match = new Regex(@"fpcompst.c\d\d");
            Regex error = new Regex(@"\[E\]");
            while ((line = sr.ReadLine()) != null)
            {
                Match errorFound = error.Match(line);

                if (errorFound.Success == true)
                {
                    throw new Exception("Error detected in ConvertPafData");
                }

                Match matchFound = match.Match(line);

                if (matchFound.Success == true)
                {
                    logger.LogInformation("ConvertPafData processing file: " + matchFound.Value);
                }
            }
        }

        // Copy CovertPafData finished result to SMi build files folder
        File.Copy(Path.Combine(settings.WorkingPath, "Db", "Uk.txt"), Path.Combine(settings.WorkingPath, "Smi", "Uk.txt"), true);
    }

    private async Task Compile()
    {
        logger.LogInformation("Compiling converted PAF data into SMi directory");

        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("3.0", Task.Run(() => CompileRunner("3.0")));
        tasks.Add("1.9", Task.Run(() => CompileRunner("1.9")));

        await Task.WhenAll(tasks.Values);
    }

    private async Task Output()
    {
        logger.LogInformation("Moving files to output directory");

        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("3.0", Task.Run(() => OutputRunner("3.0")));
        tasks.Add("1.9", Task.Run(() => OutputRunner("1.9")));

        await Task.WhenAll(tasks.Values);
    }

    private void DeployToAp()
    {
        logger.LogInformation("Shutting down RAFArgosyMaster, moving files to Argosy Post Sync folder");

        if (!isElevated)
        {
            logger.LogWarning("Did not move files, application needs administrator privledges to stop RAFArgosyMaster");
            return;
        }

        ServiceController sc = new ServiceController("RAFArgosyMaster");
        
        // Check that service is stopped, if not stop it
        if (!sc.Status.Equals(ServiceControllerStatus.Stopped))
        {
            sc.Stop();
        }

        // Copy files from output folder to AP sync folder
        Utils.CopyFiles(Path.Combine(settings.OutputPath, "3.0"), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match-i");
        
        // Start back up the service
        sc.Start();
    }



    // Helpers
    private void CompileRunner(string version)
    {
        Directory.CreateDirectory(settings.WorkingPath + @"\" + version);

        List<string> smiFiles = new List<string> { @"UK_RM_CM.xml", @"UK_RM_CM_Patterns.xml", @"UK_RM_CM_Patterns.exml", @"UK_RM_CM_Settings.xml", @"UK_RM_CM.lcs", @"BFPO.txt", @"UK.txt", @"Country.txt", @"County.txt", @"PostTown.txt", @"StreetDescriptor.txt", @"StreetName.txt", @"PoBoxName.txt", @"SubBuildingDesignator.txt", @"OrganizationName.txt", @"Country_Alias.txt", @"UK_IgnorableWordsTable.txt", @"UK_WordMatchTable.txt" };
        foreach (string file in smiFiles)
        {
            File.Copy(settings.WorkingPath + @"\Smi\" + file, settings.WorkingPath + @"\" + version + @"\" + file, true);
        }

        string directoryDataCompilerFileName = Utils.WrapQuotes(Path.Combine(settings.BuildToolsPath, version, "DirectoryDataCompiler.exe"));
        string directoryDataCompilerArgs = @"--definition " + Utils.WrapQuotes(Path.Combine(settings.WorkingPath, version, "UK_RM_CM.xml")) + @" --patterns " + Utils.WrapQuotes(Path.Combine(settings.WorkingPath, version, "UK_RM_CM_Patterns.xml")) + @" --password M0ntyPyth0n --licensed";

        Process directoryDataCompiler = Utils.RunProc(directoryDataCompilerFileName, directoryDataCompilerArgs);
        using (StreamReader sr = directoryDataCompiler.StandardOutput)
        {
            string line;
            int linesRead;
            Regex match = new Regex(@"\d\d\d\d\d");
            Regex error = new Regex(@"\[E\]");
            while ((line = sr.ReadLine()) != null)
            {
                Match errorFound = error.Match(line);

                if (errorFound.Success == true)
                {
                    throw new Exception("Error detected in DirectoryDataCompiler " + version);
                }

                Match matchFound = match.Match(line);

                if (matchFound.Success == true)
                {
                    linesRead = int.Parse(matchFound.Value);
                    if (linesRead % 5000 == 0)
                    {
                        logger.LogInformation("DirectoryDataCompiler " + version + " addresses processed: " + matchFound.Value);
                    }
                }
            }
        }
    }

    private void OutputRunner(string version)
    {
        Directory.CreateDirectory(Path.Combine(settings.OutputPath, version, "UK_RM_CM"));

        List<string> smiFiles = new List<string> { @"UK_IgnorableWordsTable.txt", @"UK_RM_CM_Patterns.exml", @"UK_WordMatchTable.txt", @"UK_RM_CM.lcs", @"UK_RM_CM.smi" };
        foreach (string file in smiFiles)
        {
            File.Copy(Path.Combine(settings.WorkingPath, version, file), Path.Combine(settings.OutputPath, version, "UK_RM_CM", file), true);
        }
        File.Copy(Path.Combine(settings.WorkingPath, version, "UK_RM_CM_Settings.xml"), Path.Combine(settings.OutputPath, version, "UK_RM_CM_Settings.xml"), true);
    }
}