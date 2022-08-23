using System.Diagnostics;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace IoMDirectoryBuilder.Common;

public class PafBuilder
{
    public Settings Settings { get; set; }

    public CancellationToken StoppingToken { get; set; }
    public Action<string> ReportStatus { get; set; }
    public Action<int, bool> ReportProgress { get; set; }

    public void Cleanup(bool clearOutput)
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("Cleaning up from previous builds");

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
        Directory.CreateDirectory(Settings.WorkingPath);
        Directory.CreateDirectory(Settings.OutputPath);

        DirectoryInfo wp = new(Settings.WorkingPath);
        DirectoryInfo op = new(Settings.OutputPath);

        // On a "clearOutput" clear out OutputPath folder in addition to WorkingPath
        if (clearOutput)
        {
            foreach (FileInfo file in op.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in op.GetDirectories())
            {
                dir.Delete(true);
            }

            Directory.Delete(Settings.OutputPath, true);
        }

        foreach (FileInfo file in wp.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in wp.GetDirectories())
        {
            dir.Attributes &= ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }

        Directory.Delete(Settings.WorkingPath, true);

        ReportProgress(1, true);
    }

    public void ConvertPafData()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("Converting PAF data");

        // Move address data files to working folder "Db"
        Directory.CreateDirectory(Settings.WorkingPath);

        foreach (KeyValuePair<string, string> file in Settings.PafLocations)
        {
            if (StoppingToken.IsCancellationRequested)
            {
                return;
            }

            File.Copy(file.Value, Path.Combine(Settings.WorkingPath, file.Key), true);
        }

        // Start ConvertPafData tool, listen for output
        string convertPafDataFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "ConvertPafData.exe"));
        string convertPafDataArgs = "--pafPath " + Utils.WrapQuotes(Settings.WorkingPath) + " --lastPafFileNum 15";

        Process convertPafData = Utils.RunProc(convertPafDataFileName, convertPafDataArgs);
        StoppingToken.Register(() => convertPafData.Close());

        using StreamReader sr = convertPafData.StandardOutput;
        string line;
        Regex match = new(@"fpcompst.c\d\d");
        Regex error = new(@"\[E\]");
        while ((line = sr.ReadLine()) != null)
        {
            Match errorFound = error.Match(line);

            if (errorFound.Success)
            {
                throw new Exception("Error detected in ConvertPafData");
            }

            Match matchFound = match.Match(line);

            if (matchFound.Success)
            {
                ReportStatus("ConvertPafData processing: " + matchFound.Value);
                ReportProgress(4, true);
            }
        }
    }

    public async Task Compile()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("Compiling converted data into SMi");

        Dictionary<string, Task> tasks = new()
    {
        { "IoM", Task.Run(() => CompileRunner(), StoppingToken) },
    };

        await Task.WhenAll(tasks.Values);
    }

    public async Task Output(bool deployToAp)
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("Moving files to output directory");

        Dictionary<string, Task> tasks = new()
    {
        { "IoM", Task.Run(() => OutputRunner(), StoppingToken) },
    };

        await Task.WhenAll(tasks.Values);

        if (deployToAp)
        {
            await DeployRunner();
        }
    }

    // Helpers
    private void CompileRunner()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        List<string> smiFiles = new() { "IsleOfMan.xml", "IsleOfMan_Patterns.exml", "IsleOfMan_Settings.xml", "BFPO.txt", "Country.txt", "County.txt", "PostTown.txt", "StreetDescriptor.txt", "StreetName.txt", "PoBoxName.txt", "SubBuildingDesignator.txt", "OrganizationName.txt", "Country_Alias.txt", "IsleOfMan_IgnorableWordsTable.txt", "IsleOfMan_WordMatchTable.txt", "IsleOfMan_CharMatchTable.txt" };
        foreach (string file in smiFiles)
        {
            if (StoppingToken.IsCancellationRequested)
            {
                return;
            }

            File.Copy(Path.Combine(Settings.SmiFilesPath, file), Path.Combine(Settings.WorkingPath, file), true);
        }

        // Start DirectoryDataCompiler tool, listen for output
        string directoryDataCompilerFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "DirectoryDataCompiler.exe"));
        string directoryDataCompilerArgs = "--definition " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "IsleOfMan.xml")) + " --patterns " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "IsleOfMan_Patterns.exml"));

        Process directoryDataCompiler = Utils.RunProc(directoryDataCompilerFileName, directoryDataCompilerArgs);
        StoppingToken.Register(() => directoryDataCompiler.Close());

        using StreamReader sr = directoryDataCompiler.StandardOutput;
        string line;
        int linesRead;
        Regex match = new(@"\d\d\d\d\d");
        Regex error = new(@"\[E\]");
        while ((line = sr.ReadLine()) != null)
        {
            Match errorFound = error.Match(line);

            if (errorFound.Success)
            {
                throw new Exception("Error detected in DirectoryDataCompiler ");
            }

            Match matchFound = match.Match(line);

            if (matchFound.Success)
            {
                linesRead = int.Parse(matchFound.Value);
                if (linesRead % 5000 == 0)
                {
                    ReportStatus("Compiler addresses processed: " + matchFound.Value);
                    ReportProgress(7, true);
                }
            }
        }
    }

    private void OutputRunner()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        Directory.CreateDirectory(Settings.OutputPath);

        List<string> smiFiles = new() { "IsleOfMan.xml", "IsleOfMan_Patterns.exml", "IsleOfMan_Settings.xml", "IsleOfMan.smi", "IsleOfMan_IgnorableWordsTable.txt", "IsleOfMan_WordMatchTable.txt", "IsleOfMan_CharMatchTable.txt" };
        foreach (string file in smiFiles)
        {
            if (StoppingToken.IsCancellationRequested)
            {
                return;
            }

            File.Copy(Path.Combine(Settings.WorkingPath, file), Path.Combine(Settings.OutputPath, file), true);
        }

        ReportProgress(99, false);
    }

    private async Task DeployRunner()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("Installing directory to Argosy Post");

        ServiceController rafMaster = new("RAFArgosyMaster");

        // Check that service is stopped, if not attempt to stop it
        if (!rafMaster.Status.Equals(ServiceControllerStatus.Stopped))
        {
            rafMaster.Stop(true);
        }

        // With timeout wait until service actually stops. ServiceController annoyingly returns control immediately, also doesn't allow SC.Stop() on a stopped/stopping service without throwing Exception
        int timeOut = 0;
        while (true)
        {
            rafMaster.Refresh();

            if (timeOut > 20)
            {
                throw new Exception("Unable to stop RAF services");
            }

            if (!rafMaster.Status.Equals(ServiceControllerStatus.Stopped))
            {
                await Task.Delay(TimeSpan.FromSeconds(1), StoppingToken);
                timeOut++;
                continue;
            }

            break;
        }

        // Copy files from output folder to AP sync folder
        Utils.CopyFiles(Settings.OutputPath, @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match-i", StoppingToken);

        // Start back up the service
        rafMaster.Start();
    }
}