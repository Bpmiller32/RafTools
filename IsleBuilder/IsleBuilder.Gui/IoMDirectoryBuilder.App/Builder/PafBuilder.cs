using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace IoMDirectoryBuilder.App;

public class PafBuilder
{
    public Settings Settings { get; set; }
    public Action<string> ReportStatus { get; set; }
    public Action<int, bool> ReportProgress { get; set; }

    public void Cleanup(bool clearOutput)
    {
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
        ReportStatus("Converting PAF data");

        // Move address data files to working folder "Db"
        Utils.CopyFiles(Path.Combine(Settings.PafFilesPath, "PAF COMPRESSED STD"), Settings.WorkingPath);
        Utils.CopyFiles(Path.Combine(Settings.PafFilesPath, "ALIAS"), Settings.WorkingPath);

        // Start ConvertPafData tool, listen for output
        string convertPafDataFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "ConvertPafData.exe"));
        string convertPafDataArgs = "--pafPath " + Utils.WrapQuotes(Settings.WorkingPath) + " --lastPafFileNum 15";

        Process convertPafData = Utils.RunProc(convertPafDataFileName, convertPafDataArgs);

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
        ReportStatus("Compiling converted data into SMi");

        Dictionary<string, Task> tasks = new()
        {
            { "IoM", Task.Run(() => CompileRunner()) },
            // { "3.0", Task.Run(() => CompileRunner("3.0")) },
            // { "1.9", Task.Run(() => CompileRunner("1.9")) }
        };

        await Task.WhenAll(tasks.Values);
    }

    public async Task Output(bool deployToAp)
    {
        ReportStatus("Moving files to output directory");

        Dictionary<string, Task> tasks = new()
        {
            { "IoM", Task.Run(() => OutputRunner()) },
            // { "3.0", Task.Run(() => OutputRunner("3.0")) }
            // { "1.9", Task.Run(() => OutputRunner("1.9")) }
        };

        await Task.WhenAll(tasks.Values);

        if (deployToAp)
        {
            DeployRunner();
        }
    }

    // Helpers
    private void CompileRunner()
    {
        List<string> smiFiles = new() { "IsleOfMan.xml", "IsleOfMan_Patterns.exml", "IsleOfMan_Settings.xml", "BFPO.txt", "Country.txt", "County.txt", "PostTown.txt", "StreetDescriptor.txt", "StreetName.txt", "PoBoxName.txt", "SubBuildingDesignator.txt", "OrganizationName.txt", "Country_Alias.txt", "IsleOfMan_IgnorableWordsTable.txt", "IsleOfMan_WordMatchTable.txt" };
        foreach (string file in smiFiles)
        {
            File.Copy(Path.Combine(Settings.SmiFilesPath, file), Path.Combine(Settings.WorkingPath, file), true);
        }

        // Start DirectoryDataCompiler tool, listen for output
        string directoryDataCompilerFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "DirectoryDataCompiler.exe"));
        string directoryDataCompilerArgs = "--definition " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "IsleOfMan.xml")) + " --patterns " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "IsleOfMan_Patterns.exml")) + " --password M0ntyPyth0n --licensed";

        Process directoryDataCompiler = Utils.RunProc(directoryDataCompilerFileName, directoryDataCompilerArgs);

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
        Directory.CreateDirectory(Settings.OutputPath);

        List<string> smiFiles = new() { "IsleOfMan.xml", "IsleOfMan_Patterns.exml", "IsleOfMan_Settings.xml", "IsleOfMan.smi", "IsleOfMan_IgnorableWordsTable.txt", "IsleOfMan_WordMatchTable.txt" };
        foreach (string file in smiFiles)
        {
            File.Copy(Path.Combine(Settings.WorkingPath, file), Path.Combine(Settings.OutputPath, file), true);
        }

        ReportProgress(99, false);
    }

    private void DeployRunner()
    {
        ReportStatus("Installing directory to Argosy Post");

        ServiceController sc = new("RAFArgosyMaster");

        // Check that service is stopped, if not stop it
        if (!sc.Status.Equals(ServiceControllerStatus.Stopped))
        {
            sc.Stop();
        }

        // Copy files from output folder to AP sync folder
        Utils.CopyFiles(Settings.OutputPath, @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match-i");

        // Start back up the service
        sc.Start();
    }
}
