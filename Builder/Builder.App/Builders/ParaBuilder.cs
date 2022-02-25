using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Builder.App.Utils;

namespace Builder.App.Builders;

public class ParaBuilder
{
    private readonly string inputPath;
    private readonly string workingPath;
    private readonly string outputPath;
    private readonly Settings settings;
    private readonly DatabaseContext context;
    private readonly Action<int> progress;
    private string month;
    private string year;

    public ParaBuilder(Settings settings, DatabaseContext context, Action<int> progress)
    {
        this.inputPath = settings.AddressDataPath;
        this.workingPath = settings.WorkingPath;
        this.outputPath = settings.OutputPath;
        this.settings = settings;
        this.context = context;
        this.progress = progress;

        this.month = settings.DataMonth;
        this.year = settings.DataYear;
    }

    public void CheckInput()
    {
        if (!Directory.EnumerateFileSystemEntries(inputPath).Any())
        {
            throw new Exception("No files to work with in input");
        }
        if (!Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory() + @"\BuildUtils").Any())
        {
            throw new Exception("BuildUtils folder is missing");
        }

        progress(1);
    }

    public void ExtractDownload()
    {
        DirectoryInfo ip = new DirectoryInfo(inputPath);
        
        foreach (DirectoryInfo dir in ip.GetDirectories())
        {
            dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }

        ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"Files.zip"), inputPath);
    }

    public void Cleanup(bool fullClean)
    {
        Utils.Utils.KillPsProcs();

        Directory.CreateDirectory(workingPath);
        Directory.CreateDirectory(outputPath);

        DirectoryInfo wp = new DirectoryInfo(workingPath);
        DirectoryInfo op = new DirectoryInfo(outputPath);

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

            Directory.Delete(workingPath, true);

            progress(2);
            return;
        }

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

        progress(1);
    }

    public async Task Extract()
    {
        string shortYear = year.Substring(2, 2);

        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("zip", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"ads6", @"ads_zip_09_" + month + shortYear + ".exe"), Path.Combine(workingPath, @"zip"));
            File.Create(Path.Combine(workingPath, @"zip", @"live.txt")).Close();
        }));
        tasks.Add("lacs", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"DPVandLACS", @"LACSLink", @"ads_lac_09_" + month + shortYear + ".exe"), Path.Combine(workingPath, @"lacs"));
            File.Create(Path.Combine(workingPath, @"lacs", @"live.txt")).Close();
        }));
        tasks.Add("suite", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"DPVandLACS", @"SuiteLink", @"ads_slk_09_" + month + shortYear + ".exe"), Path.Combine(workingPath, @"suite"));
            File.Create(Path.Combine(workingPath, @"suite", @"live.txt")).Close();
        }));
        tasks.Add("dpv", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(inputPath, @"DPVandLACS", @"DPVfull", @"ads_dpv_09_" + month + shortYear + ".exe"), Path.Combine(workingPath, @"dpv"));
            File.Create(Path.Combine(workingPath, @"dpv", @"live.txt")).Close();

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Directory.GetCurrentDirectory() + @"\BuildUtils\PDBIntegrity.exe",
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

        progress(18);
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

        progress(78);
    }

    public void CheckBuildComplete()
    {
        // Will be null if Crawler never made a record for it, watch out if running standalone
        ParaBundle bundle = context.ParaBundles.Where(x => (int.Parse(month) == x.DataMonth) && (int.Parse(year) == x.DataYear)).FirstOrDefault();
        bundle.IsBuildComplete = true;

        context.SaveChanges();
    }
}
