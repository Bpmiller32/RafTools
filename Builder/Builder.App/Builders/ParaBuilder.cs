using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Builder.App.Builders;

public class ParaBuilder
{
    private readonly string inputPath;
    private readonly string workingPath;
    private readonly string outputPath;
    private string month;
    private string year;

    public ParaBuilder(string inputPath, string workingPath, string outputPath)
    {
        this.inputPath = inputPath;
        this.workingPath = workingPath;
        this.outputPath = outputPath;
    }

    public void CheckInput()
    {
        if (!Directory.EnumerateFileSystemEntries(inputPath).Any())
        {
            throw new Exception("No files to work with in input");
        }
        if (!Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory() + @"\Utils").Any())
        {
            throw new Exception("Utils folder is missing");
        }
    }

    public void Cleanup()
    {
        Utils.Utils.KillPsProcs();

        Directory.CreateDirectory(inputPath);
        Directory.CreateDirectory(workingPath);
        Directory.CreateDirectory(outputPath);

        DirectoryInfo wp = new DirectoryInfo(workingPath);
        DirectoryInfo op = new DirectoryInfo(outputPath);

        foreach (var file in wp.GetFiles())
        {
            file.Delete();
        }
        foreach (var dir in wp.GetDirectories())
        {
            dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }
        foreach (var file in op.GetFiles())
        {
            file.Delete();
        }
        foreach (var dir in op.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    public void FindDate()
    {
        using (StreamReader sr = new StreamReader(Path.Combine(inputPath, @"ads6", @"readme.txt")))
        {
            string line;
            Regex regex = new Regex(@"(Issue Date:)(\s+)(\d\d\/\d\d\/\d\d\d\d)");

            while ((line = sr.ReadLine()) != null)
            {
                Match match = regex.Match(line);

                if (match.Success == true)
                {
                    year = match.Groups[3].Value.Substring(8, 2);
                    month = match.Groups[3].Value.Substring(0, 2);
                }
            }
        }

        if (month == null || year == null)
        {
            throw new Exception("Month/date not found in input files");
        }
    }

    public async Task Extract()
    {
        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("zip", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"ads6", @"ads_zip_09_" + month + year + ".exe"), Path.Combine(workingPath, @"zip"));
            File.Create(Path.Combine(workingPath, @"zip", @"live.txt")).Close();
        }));
        tasks.Add("lacs", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"DPVandLACS", @"LACSLink", @"ads_lac_09_" + month + year + ".exe"), Path.Combine(workingPath, @"lacs"));
            File.Create(Path.Combine(workingPath, @"lacs", @"live.txt")).Close();
        }));
        tasks.Add("suite", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"DPVandLACS", @"SuiteLink", @"ads_slk_09_" + month + year + ".exe"), Path.Combine(workingPath, @"suite"));
            File.Create(Path.Combine(workingPath, @"suite", @"live.txt")).Close();
        }));
        tasks.Add("dpv", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"DPVandLACS", @"DPVfull", @"ads_dpv_09_" + month + year + ".exe"), Path.Combine(workingPath, @"dpv"));
            File.Create(Path.Combine(workingPath, @"dpv", @"live.txt")).Close();

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Directory.GetCurrentDirectory() + @"\Utils\PDBIntegrity.exe",
                Arguments = Path.Combine(workingPath, @"dpv", @"fileinfo_log.txt"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            Process proc = new Process()
            {
                StartInfo = startInfo
            };

            proc.Start();

            using (StreamReader sr = proc.StandardOutput)
            {
                string procOutput = sr.ReadToEnd();
                if (!procOutput.Contains("Database files are consistent"))
                {
                    throw new Exception("Database files are NOT consistent");
                };
            }
        }));

        await Task.WhenAll(tasks.Values);
    }

    public async Task Archive()
    {
        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("zip", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(outputPath, @"Zip4"));
            ZipFile.CreateFromDirectory(Path.Combine(workingPath, @"zip"), Path.Combine(outputPath, @"Zip4", @"Zip4.zip"));
        }));
        tasks.Add("dpv", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(outputPath, @"DPV"));
            ZipFile.CreateFromDirectory(Path.Combine(workingPath, @"dpv"), Path.Combine(outputPath, @"DPV", @"DPV.zip"));
        }));
        tasks.Add("suite", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(outputPath, @"Suite"));
            ZipFile.CreateFromDirectory(Path.Combine(workingPath, @"suite"), Path.Combine(outputPath, @"Suite", @"SUITE.zip"));
        }));
        tasks.Add("lacs", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(outputPath, @"LACS"));
            foreach (var file in Directory.GetFiles(Path.Combine(workingPath, @"lacs")))
            {
                File.Copy(file, Path.Combine(Path.Combine(outputPath, @"LACS"), Path.GetFileName(file)));
            }
        }));

        await Task.WhenAll(tasks.Values);
    }
}
