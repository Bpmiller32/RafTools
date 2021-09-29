using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace OverwatchApi.Data
{
    // ✔ add reader for readme file/some logic to know the month and year of the files
    // ✔ research and add logic to accomidate dpv integrity executable on dpv task
    // ✔ better way to catch exceptions, send them up to the main call procedure
    // ✔ add compress functionality to output folder
    // ✔ add cleanup function (redo and recall prep to cleanup?)
    // ✔ add Progress<T> functionality and think about how this object will be exported and work with API
    // - Check for Utils folder in Cleanup()
    // - Change FindDate to use regex for better result, add error checking
    // - Add logic to Cleanup to kill existing process similar to RoyalWorker
    public class ParascriptWorker
    {
        private readonly string inputPath;
        private readonly string workingPath;
        private readonly string outputPath;
        private readonly IProgress<int> progress;
        private string month;
        private string year;

        public ParascriptWorker(string inputPath, string workingPath, string outputPath, IProgress<int> progress)
        {
            this.inputPath = inputPath;
            this.workingPath = workingPath;
            this.outputPath = outputPath;
            this.progress = progress;
        }

        public bool Cleanup()
        {
            try
            {
                Directory.CreateDirectory(inputPath);
                Directory.CreateDirectory(workingPath);
                Directory.CreateDirectory(outputPath);

                if (!Directory.EnumerateFileSystemEntries(inputPath).Any())
                {
                    throw new Exception("No files to work with in input");
                }
                foreach (var dir in Directory.GetDirectories(workingPath))
                {
                    Directory.Delete(dir, true);
                }
                foreach (var dir in Directory.GetDirectories(outputPath))
                {
                    Directory.Delete(dir, true);
                }

                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool FindDate()
        {
            try
            {
                using (StreamReader sr = new StreamReader(inputPath + @"\ads6\readme.txt"))
                {
                    sr.ReadLine();
                    string output = sr.ReadLine();
                    month = output.Substring(19, 2);
                    year = output.Substring(27, 2);
                }

                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public async Task<bool> Extract()
        {
            try
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();

                tasks.Add("zip", Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(inputPath + @"\ads6\ads_zip_09_" + month + year + ".exe", workingPath + @"\zip");
                    File.Create(workingPath + @"\zip\live.txt").Close();

                    progress.Report(3);
                }));
                tasks.Add("lacs", Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(inputPath + @"\DPVandLACS\LACSLink\ads_lac_09_" + month + year + ".exe", workingPath + @"\lacs");
                    File.Create(workingPath + @"\lacs\live.txt").Close();

                    progress.Report(3);
                }));
                tasks.Add("suite", Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(inputPath + @"\DPVandLACS\SuiteLink\ads_slk_09_" + month + year + ".exe", workingPath + @"\suite");
                    File.Create(workingPath + @"\suite\live.txt").Close();

                    progress.Report(4);
                }));
                tasks.Add("dpv", Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(inputPath + @"\DPVandLACS\DPVfull\ads_dpv_09_" + month + year + ".exe", workingPath + @"\dpv");
                    File.Create(workingPath + @"\dpv\live.txt").Close();

                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = Directory.GetCurrentDirectory() + @"\Utils\PDBIntegrity.exe",
                        Arguments = workingPath + @"\dpv\fileinfo_log.txt",
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
                            throw new Exception("bad");
                        };
                    }
                    progress.Report(12);
                }));

                await Task.WhenAll(tasks.Values);

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public async Task<bool> Archive()
        {
            try
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();

                tasks.Add("zip", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\Zip4");
                    ZipFile.CreateFromDirectory(workingPath + @"\zip", outputPath + @"\Zip4\Zip4.zip");

                    progress.Report(30);
                }));
                tasks.Add("dpv", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\DPV");
                    ZipFile.CreateFromDirectory(workingPath + @"\dpv", outputPath + @"\DPV\DPV.zip");

                    progress.Report(30);
                }));
                tasks.Add("suite", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\Suite");
                    ZipFile.CreateFromDirectory(workingPath + @"\suite", outputPath + @"\Suite\SUITE.zip");

                    progress.Report(15);
                }));
                tasks.Add("lacs", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\LACS");
                    foreach (var file in Directory.GetFiles(workingPath + @"\lacs"))
                    {
                        File.Copy(file, Path.Combine(outputPath + @"\LACS", Path.GetFileName(file)));
                    }

                    progress.Report(1);
                }));

                await Task.WhenAll(tasks.Values);

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}