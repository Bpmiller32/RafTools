using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace OverwatchApi.Data
{
    // ✔ Add logic to catch errors in executables (look for [E] in regex)
    // ✔ Add to Cleaup a process killer for open process that could stop file/folder deletion
    // ✔ Remove dongle folder from workingPath
    // ✔ Change finddate to use regex for better consistency
    // ✔ Add error checking to FindDate
    // ✔ Add progress reports, remove stopwatch markers
    // ✔ Change utils to be in there own folder
    // ✔ Check for Utils folder
    // - Add check for svn credentials
    public class RoyalWorker
    {
        private readonly string inputPath;
        private readonly string workingPath;
        private readonly string outputPath;
        private readonly IProgress<int> progress;
        private string month;
        private string year;

        public RoyalWorker(string inputPath, string workingPath, string outputPath, IProgress<int> progress)
        {
            this.inputPath = inputPath;
            this.workingPath = workingPath;
            this.outputPath = outputPath;
            this.progress = progress;
        }

        private Process RunProc(string fileName, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process proc = new Process()
            {
                StartInfo = startInfo
            };

            proc.Start();

            return proc;
        }

        public bool CheckInput()
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(inputPath).Any())
                {
                    throw new Exception("No files to work with in input");
                }
                if (!Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory() + @"\Utils").Any())
                {
                    throw new Exception("Utils folder is missing");
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool Cleanup()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("ConvertPafData"))
                {
                    process.Kill(true);
                }
                foreach (var process in Process.GetProcessesByName("DirectoryDataCompiler"))
                {
                    process.Kill(true);
                }

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
                using (StreamReader sr = new StreamReader(inputPath + @"\PAF COMPRESSED STD\README.txt"))
                {
                    string line;
                    Regex regex = new Regex(@"(Version : )(Y\d\dM\d\d)");

                    while ((line = sr.ReadLine()) != null)
                    {
                        Match match = regex.Match(line);

                        if (match.Success == true)
                        {
                            year = match.Groups[2].Value.Substring(1, 2);
                            month = match.Groups[2].Value.Substring(4, 2);
                        }
                    }
                }

                if (month == null || year == null)
                {
                    throw new Exception("Month/date not found in input files");
                }

                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool UpdateSmiFiles()
        {
            try
            {
                Directory.CreateDirectory(workingPath + @"\Smi");

                Process smiCheckout = RunProc(@"C:\Program Files\TortoiseSVN\bin\svn.exe", @"export https://scm.raf.com/repos/tags/TechServices/Tag24-UK_RM_CM-3.0/Directory_Creation_Files --username billym " + workingPath + @"\Smi" + " --force");
                smiCheckout.WaitForExit();

                Process dongleCheckout = RunProc(@"C:\Program Files\TortoiseSVN\bin\svn.exe", @"export https://scm.raf.com/repos/trunk/TechServices/SMI/Directories/UK/DongleList --username billym " + workingPath + @"\Smi" + " --force");
                dongleCheckout.WaitForExit();

                // Edit SMi definition xml file with updated date 
                XmlDocument defintionFile = new XmlDocument();
                defintionFile.Load(workingPath + @"\Smi\UK_RM_CM.xml");
                XmlNode root = defintionFile.DocumentElement;
                root.Attributes[1].Value = @"Y" + year + @"M" + month;
                defintionFile.Save(workingPath + @"\Smi\UK_RM_CM.xml");

                // Edit Uk dongle list with updated date
                using (StreamWriter sw = new StreamWriter(workingPath + @"\Smi\DongleTemp.txt"))
                {
                    sw.WriteLine(@"Date=20" + year + month + @"19");

                    using (StreamReader sr = new StreamReader(workingPath + @"\Smi\UK_RM_CM.txt"))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }

                File.Delete(workingPath + @"\Smi\UK_RM_CM.txt");
                File.Move(workingPath + @"\Smi\DongleTemp.txt", workingPath + @"\Smi\UK_RM_CM.txt");

                // Encrypt new Uk dongle list
                Process encryptRep = RunProc(Directory.GetCurrentDirectory() + @"\Utils\EncryptREP.exe", @"-x lcs " + workingPath + @"\Smi\UK_RM_CM.txt");
                encryptRep.WaitForExit();

                // Encrypt patterns
                Process encryptPatterns = RunProc(Directory.GetCurrentDirectory() + @"\Utils\EncryptPatterns", @"--patterns " + workingPath + @"\Smi\UK_RM_CM_Patterns.xml --clickCharge");
                encryptPatterns.WaitForExit();

                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool ConvertPafData()
        {
            try
            {
                Directory.CreateDirectory(workingPath + @"\Db");
                foreach (var file in Directory.GetFiles(inputPath + @"\PAF COMPRESSED STD"))
                {
                    File.Copy(file, Path.Combine(workingPath + @"\Db", Path.GetFileName(file)), true);
                }
                File.Copy(inputPath + @"\ALIAS\aliasfle.c01", workingPath + @"\Db\aliasfle.c01", true);


                Process convertPafData = RunProc(Directory.GetCurrentDirectory() + @"\Utils\ConvertPafData.exe", @"--pafPath " + workingPath + @"\Db --lastPafFileNum 15");

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
                            progress.Report(3);
                        }
                    }
                }

                File.Copy(workingPath + @"\Db\Uk.txt", workingPath + @"\Smi\Uk.txt", true);

                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public async Task<bool> Compile()
        {
            try
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();

                tasks.Add("3.0", Task.Run((Action)(() =>
                {
                    Directory.CreateDirectory(workingPath + @"\3.0");

                    List<string> smiFiles = new List<string> { @"UK_RM_CM.xml", @"UK_RM_CM_Patterns.xml", @"UK_RM_CM_Patterns.exml", @"UK_RM_CM_Settings.xml", @"UK_RM_CM.lcs", @"BFPO.txt", @"UK.txt", @"Country.txt", @"County.txt", @"PostTown.txt", @"StreetDescriptor.txt", @"StreetName.txt", @"PoBoxName.txt", @"SubBuildingDesignator.txt", @"OrganizationName.txt", @"Country_Alias.txt", @"UK_IgnorableWordsTable.txt", @"UK_WordMatchTable.txt" };
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\Smi\" + file, workingPath + @"\3.0\" + file, true);
                    }

                    Process directoryDataCompiler = RunProc(Directory.GetCurrentDirectory() + @"\Utils\3.0\DirectoryDataCompiler.exe", @"--definition " + workingPath + @"\3.0\UK_RM_CM.xml --patterns " + workingPath + @"\3.0\UK_RM_CM_Patterns.xml --password M0ntyPyth0n --licensed");
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
                                throw new Exception("Error detected in DirectoryDataCompiler 3.0");
                            }

                            Match matchFound = match.Match(line);

                            if (matchFound.Success == true)
                            {
                                linesRead = int.Parse(matchFound.Value);
                                if (linesRead % 5000 == 0)
                                {
                                    progress.Report(5);
                                }
                            }
                        }
                    }
                })));
                tasks.Add("1.9", Task.Run(() =>
                {
                    Directory.CreateDirectory(workingPath + @"\1.9");

                    List<string> smiFiles = new List<string> { @"UK_RM_CM.xml", @"UK_RM_CM_Patterns.xml", @"UK_RM_CM_Patterns.exml", @"UK_RM_CM_Settings.xml", @"UK_RM_CM.lcs", @"BFPO.txt", @"UK.txt", @"Country.txt", @"County.txt", @"PostTown.txt", @"StreetDescriptor.txt", @"StreetName.txt", @"PoBoxName.txt", @"SubBuildingDesignator.txt", @"OrganizationName.txt", @"Country_Alias.txt", @"UK_IgnorableWordsTable.txt", @"UK_WordMatchTable.txt" };
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\Smi\" + file, workingPath + @"\1.9\" + file, true);
                    }

                    Process directoryDataCompiler = RunProc(Directory.GetCurrentDirectory() + @"\Utils\1.9\DirectoryDataCompiler.exe", @"--definition " + workingPath + @"\1.9\UK_RM_CM.xml --patterns " + workingPath + @"\1.9\UK_RM_CM_Patterns.xml --password M0ntyPyth0n --licensed");
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
                                throw new Exception("Error detected in DirectoryDataCompiler 1.9");
                            }

                            Match matchFound = match.Match(line);

                            if (matchFound.Success == true)
                            {
                                linesRead = int.Parse(matchFound.Value);
                                if (linesRead % 5000 == 0)
                                {
                                    progress.Report(5);
                                }
                            }
                        }
                    }
                }));

                await Task.WhenAll(tasks.Values);
                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public async Task<bool> Output()
        {
            try
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();

                tasks.Add("3.0", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\3.0");
                    Directory.CreateDirectory(outputPath + @"\3.0\UK_RM_CM");

                    List<string> smiFiles = new List<string> { @"UK_IgnorableWordsTable.txt", @"UK_RM_CM_Patterns.exml", @"UK_WordMatchTable.txt", @"UK_RM_CM.lcs", @"UK_RM_CM.smi" };
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\3.0\" + file, outputPath + @"\3.0\UK_RM_CM\" + file, true);
                    }
                    File.Copy(workingPath + @"\3.0\UK_RM_CM_Settings.xml", outputPath + @"\3.0\UK_RM_CM_Settings.xml", true);
                }));
                tasks.Add("1.9", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\1.9");
                    Directory.CreateDirectory(outputPath + @"\1.9\UK_RM_CM");

                    List<string> smiFiles = new List<string> { @"UK_IgnorableWordsTable.txt", @"UK_RM_CM_Patterns.exml", @"UK_WordMatchTable.txt", @"UK_RM_CM.lcs", @"UK_RM_CM.smi" };
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\1.9\" + file, outputPath + @"\1.9\UK_RM_CM\" + file, true);
                    }
                    File.Copy(workingPath + @"\1.9\UK_RM_CM_Settings.xml", outputPath + @"\1.9\UK_RM_CM_Settings.xml", true);
                }));

                await Task.WhenAll(tasks.Values);

                progress.Report(1);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}