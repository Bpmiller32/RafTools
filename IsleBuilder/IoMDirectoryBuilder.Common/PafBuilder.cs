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
        Utils.KillRmProcs();

        // Ensure working and output directories are created and clear them if they already exist
        Directory.CreateDirectory(Settings.WorkingPath);
        Directory.CreateDirectory(Settings.OutputPath);

        DirectoryInfo wp = new(Settings.WorkingPath);
        DirectoryInfo op = new(Settings.OutputPath);

        // On a "clearOutput" clear out OutputPath folder and sqlite db file in addition to WorkingPath
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

            // Delete sqlite db file if it exists for ConvertMainFile
            File.Delete(Path.Combine(Settings.SmiFilesPath, "db.sqlite"));
        }

        // Always clear out WorkingPath folder
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
    }

    public void ConvertMainFile()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("-- Converting PAF Main File --");

        // Make sure WorkingPath exists for generated files to land
        Directory.CreateDirectory(Settings.WorkingPath);

        // Start ConvertMainFile utility
        string convertMainFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "ConvertMainfileToCompStdConsole.exe"));
        string convertMainFileArgs = Utils.WrapQuotes(Path.Combine(Settings.PafFilesPath, "PAF MAIN FILE")) + " " + Utils.WrapQuotes(Settings.WorkingPath);

        Process convertMainFile = Utils.RunProc(convertMainFileName, convertMainFileArgs);
        StoppingToken.Register(() => convertMainFile.Close());

        // Listen for output from utility
        using StreamReader sr = convertMainFile.StandardOutput;

        string line;
        Regex matchRecordBuilding = new("(Loading )(.+)(...)");
        Regex matchFpBuilding = new(@"(Writing to )(.+)(fpcompst.c\d\d)(...)");
        Regex error = new("(error)");
        while ((line = sr.ReadLine()) != null)
        {
            Match errorFound = error.Match(line);

            if (errorFound.Success)
            {
                throw new Exception("Error detected in ConvertMainFile");
            }

            Match matchFoundRecordBuilding = matchRecordBuilding.Match(line);
            Match matchFoundFpBuilding = matchFpBuilding.Match(line);

            if (matchFoundRecordBuilding.Success)
            {
                ReportStatus("ConvertMainFile processing records: " + matchFoundRecordBuilding.Groups[2].Value);
            }

            if (matchFoundFpBuilding.Success)
            {
                ReportStatus("ConvertMainFile creating fpcompst files: " + matchFoundFpBuilding.Groups[3].Value);
            }
        }
    }

    public void ConvertPafData()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("-- Converting PAF data --");

        // Make sure WorkingPath exists for generated files to land
        Directory.CreateDirectory(Settings.WorkingPath);

        // Copy/Move files needed for ConvertPafData into flat structure in WorkingPath from both Compressed and Main PAF
        Utils.CopyFiles(Path.Combine(Settings.PafFilesPath, "ALIAS"), Settings.WorkingPath, StoppingToken);
        Utils.MoveFiles(Path.Combine(Settings.WorkingPath, "PAF COMPRESSED STD"), Settings.WorkingPath, StoppingToken);

        // Start ConvertPafData utility
        string convertPafDataFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "ConvertPafData.exe"));
        string convertPafDataArgs = "--pafPath " + Utils.WrapQuotes(Settings.WorkingPath) + " --lastPafFileNum 15";

        Process convertPafData = Utils.RunProc(convertPafDataFileName, convertPafDataArgs);
        StoppingToken.Register(() => convertPafData.Close());

        // Listen for output from utility
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
            }
        }
    }

    public void Compile()
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("-- Compiling converted data into SMi --");

        // Change request from Joe/Bluecrest
        List<string> smiFiles = Directory.GetFiles(Settings.SmiFilesPath).Select(file => Path.GetFileName(file)).ToList();
        List<string> removeFiles = new()
        {
            "ConvertMainfileToCompStdConsole.exe",
            "ConvertMainfileToCompStd.exe",
            "ConvertPafData_Log_latest.txt",
            "DataManager.dll",
            "PafConverter.dll",
            "SharedComponents.dll",
            "ConvertPafData.exe",
            "Dafs.dll",
            "DirectoryDataCompiler.exe",
            "EntityFramework.dll",
            "EntityFramework.SqlServer.dll",
            "log4net.dll",
            "MoreLinq.dll",
            "SMI.dll",
            "Smi.xsd",
            "SqliteDbManager.dll",
            "System.Data.SQLite.dll",
            "System.Data.SQLite.EF6.dll",
            "System.Data.SQLite.Linq.dll",
            "System.ValueTuple.dll",
            "UkPostProcessor.dll",
            "xerces-c_3_2.dll"
        };

        foreach (string file in removeFiles)
        {
            smiFiles.Remove(file);
        }

        // Copy all SMi files needed for DirectoryDataCompiler to working folder
        foreach (string file in smiFiles)
        {
            if (StoppingToken.IsCancellationRequested)
            {
                return;
            }

            File.Copy(Path.Combine(Settings.SmiFilesPath, file), Path.Combine(Settings.WorkingPath, file), true);
        }

        // Start DirectoryDataCompiler utility
        string directoryDataCompilerFileName = Utils.WrapQuotes(Path.Combine(Settings.SmiFilesPath, "DirectoryDataCompiler.exe"));
        string directoryDataCompilerArgs = "--definition " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "IsleOfMan.xml")) + " --patterns " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "IsleOfMan_Patterns.exml"));

        Process directoryDataCompiler = Utils.RunProc(directoryDataCompilerFileName, directoryDataCompilerArgs);
        StoppingToken.Register(() => directoryDataCompiler.Close());

        // Listen for output from utility
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
                    ReportStatus("SMi Compiler addresses processed: " + matchFound.Value);
                }
            }
        }
    }

    public async Task Output(bool deployToAp)
    {
        if (StoppingToken.IsCancellationRequested)
        {
            return;
        }

        ReportStatus("-- Moving files to output directory --");

        Directory.CreateDirectory(Settings.OutputPath);

        // Change request from Joe/Bluecrest
        List<string> workingFiles = Directory.GetFiles(Settings.WorkingPath).Select(file => Path.GetFileName(file)).ToList();
        List<string> removeFiles = new()
        {
            "UK.c01.txt",
            "UK.c02.txt",
            "UK.c03.txt",
            "UK.c04.txt",
            "UK.c05.txt",
            "UK.c06.txt",
            "UK.c07.txt",
            "UK.c08.txt",
            "UK.c09.txt",
            "UK.c10.txt",
            "UK.c11.txt",
            "UK.c12.txt",
            "UK.c13.txt",
            "UK.c14.txt",
            "UK.c15.txt",
            "UK.txt",
            "README.TXT",
            "DirectoryDataCompilerLog_latest.txt",
            "fpcompst.c01",
            "fpcompst.c02",
            "fpcompst.c03",
            "fpcompst.c04",
            "fpcompst.c05",
            "fpcompst.c06",
            "fpcompst.c07",
            "fpcompst.c08",
            "fpcompst.c09",
            "fpcompst.c10",
            "fpcompst.c11",
            "fpcompst.c12",
            "fpcompst.c13",
            "fpcompst.c14",
            "fpcompst.c15",
            "aliasfle.c01"
        };

        // Copy all SMi directory files to output folder
        foreach (string file in removeFiles)
        {
            workingFiles.Remove(file);
        }

        foreach (string file in workingFiles)
        {
            if (StoppingToken.IsCancellationRequested)
            {
                return;
            }

            File.Copy(Path.Combine(Settings.WorkingPath, file), Path.Combine(Settings.OutputPath, file), true);
        }

        // Copy all SMi directory files needed to Argosy Post folder
        if (deployToAp)
        {
            ReportStatus("Installing directory to Argosy Post");

            ServiceController rafMaster = new("RAFArgosyMaster");

            // Check that service is stopped, if not attempt to stop it
            if (!rafMaster.Status.Equals(ServiceControllerStatus.Stopped))
            {
                rafMaster.Stop(true);
            }

            // With a timeout, wait until service actually stops. ServiceController annoyingly returns control immediately, also doesn't allow SC.Stop() on a stopped/stopping service without throwing Exception
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
}