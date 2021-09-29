using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml;
using System.IO;
using System;

namespace RoyalWorker
{
    class Stop
    {
        public static Stopwatch watch = new Stopwatch();
    }

    class Program
    {
        static int Main(string[] args)
        {
            string inputPath = Directory.GetCurrentDirectory() + @"\RM-Input";
            string workingPath = Directory.GetCurrentDirectory() + @"\RM-Working";
            string outputPath = Directory.GetCurrentDirectory() + @"\RM-Output";

            Stop.watch.Start();

            int progressPercent = 0;
            Progress<int> progress = new Progress<int>((percent) =>
            {
                progressPercent += percent;
                System.Console.WriteLine("Progress: " + progressPercent);
            });

            // Everything below called by api controller
            RoyalWorker rm = new RoyalWorker(inputPath, workingPath, outputPath, progress);

            if (!rm.Cleanup())
            {
                System.Console.WriteLine("Failed Cleanup");
                return 0;
            }
            if (!rm.FindDate())
            {
                System.Console.WriteLine("Failed FindDate");
                return 0;
            }
            if (!rm.UpdateSmiFiles())
            {
                System.Console.WriteLine("Failed UpdateSmiFiles");
                return 0;
            }
            if (!rm.ConvertPafData())
            {
                System.Console.WriteLine("Failed ConvertPafData");
                return 0;
            }
            if (!rm.Compile().Result)
            {
                System.Console.WriteLine("Failed Compile");
                return 0;
            }
            if (!rm.Output().Result)
            {
                System.Console.WriteLine("Failed Output");
                return 0;
            }

            return 1;
        }
    }



// - Add logic to catch errors in executables (look for [E] in regex)
// - Change finddate to use regex for better consistency
// - Add error checking to FindDate
// - Add progress reports, remove stopwatch markers
// - Change utils to be in there own folder
    class RoyalWorker
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

        private static Process RunProc(string fileName, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            Process proc = new Process()
            {
                StartInfo = startInfo
            };

            proc.Start();

            return proc;
        }

        public bool Cleanup()
        {
            System.Console.WriteLine("Cleanup start: " + Stop.watch.Elapsed);
            try
            {   
                Directory.CreateDirectory(inputPath);
                Directory.CreateDirectory(workingPath);
                Directory.CreateDirectory(outputPath);

                if (!Directory.EnumerateFileSystemEntries(inputPath).Any())
                {
                    throw new Exception("No files to work with in input");
                }

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

                // progress.Report(1);
                System.Console.WriteLine("Cleanup end: " + Stop.watch.Elapsed);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool FindDate()
        {
            System.Console.WriteLine("FindDate start: " + Stop.watch.Elapsed);
            try
            {
                using (StreamReader sr = new StreamReader(inputPath + @"\PAF COMPRESSED STD\README.txt"))
                {
                    for (int i = 0; i < 9; i++)
                    {
                        sr.ReadLine();
                    }

                    string output = sr.ReadLine();
                    month = output.Substring(14, 2);
                    year = output.Substring(11, 2);
                }

                System.Console.WriteLine("FindDate end: " + Stop.watch.Elapsed);
                return true;                
            }
            catch (System.Exception)
            {
                return false;                
            }
        }

        public bool UpdateSmiFiles()
        {
            System.Console.WriteLine("UpdateSmiFiles start: " + Stop.watch.Elapsed);
            try
            {
                Directory.CreateDirectory(workingPath + @"\Smi");
                Directory.CreateDirectory(workingPath + @"\Dongle");

                Process smiCheckout = RunProc("svn.exe", @"export https://github.com/Bpmiller32/RafTools/trunk/svntag --username bpmiller32@gmail.com " + workingPath + @"\Smi" + " --force");
                smiCheckout.WaitForExit();

                Process dongleCheckout = RunProc("svn.exe", @"export https://github.com/Bpmiller32/RafTools/trunk/svnhead --username bpmiller32@gmail.com " + workingPath + @"\Dongle" + " --force");
                dongleCheckout.WaitForExit();


                // Edit SMi definition xml file with updated date 
                XmlDocument defintionFile = new XmlDocument();
                defintionFile.Load(workingPath + @"\Smi\UK_RM_CM.xml");
                XmlNode root = defintionFile.DocumentElement;
                root.Attributes[1].Value = @"Y" + year + @"M" + month;
                defintionFile.Save(workingPath + @"\Smi\UK_RM_CM.xml");

                // Edit Uk dongle list with updated date
                using (StreamWriter sw = new StreamWriter(workingPath + @"\Dongle\DongleTemp.txt"))
                {
                    sw.WriteLine(@"Date=20" + year + month + @"19");

                    using (StreamReader sr = new StreamReader(workingPath + @"\Dongle\UK_RM_CM.txt"))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }

                File.Delete(workingPath + @"\Dongle\UK_RM_CM.txt");
                File.Move(workingPath + @"\Dongle\DongleTemp.txt", workingPath + @"\Dongle\UK_RM_CM.txt");

                // Encrypt new Uk dongle list
                Process encryptRep = RunProc(Directory.GetCurrentDirectory() + @"\EncryptREP.exe", @"-x lcs " + workingPath + @"\Dongle\UK_RM_CM.txt");
                encryptRep.WaitForExit();
                File.Copy(workingPath + @"\Dongle\UK_RM_CM.lcs", workingPath + @"\Smi\UK_RM_CM.lcs");

                // Encrypt patterns
                Process encryptPatterns = RunProc(Directory.GetCurrentDirectory() + @"\EncryptPatterns", @"--patterns " + workingPath + @"\Smi\UK_RM_CM_Patterns.xml --clickCharge");
                encryptPatterns.WaitForExit();

                System.Console.WriteLine("UpdateSmiFiles end: " + Stop.watch.Elapsed);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool ConvertPafData() 
        {
            System.Console.WriteLine("ConvertPafData start: " + Stop.watch.Elapsed);
            try
            {
                Directory.CreateDirectory(workingPath + @"\Db");
                foreach (var file in Directory.GetFiles(inputPath + @"\PAF COMPRESSED STD"))
                {
                    File.Copy(file, Path.Combine(workingPath + @"\Db", Path.GetFileName(file)));
                }
                File.Copy(inputPath + @"\ALIAS\aliasfle.c01", workingPath + @"\Db\aliasfle.c01");
                
                
                Process convertPafData = RunProc(Directory.GetCurrentDirectory() + @"\ConvertPafData.exe", @"--pafPath " + workingPath + @"\Db --lastPafFileNum 15");

                using (StreamReader sr = convertPafData.StandardOutput)
                {
                    string line;
                    string fpNum;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.IndexOf(@"fpcompst.c") != -1)
                        {
                            fpNum = line.Substring(line.IndexOf(@"fpcompst.c") + "fpcompst.c".Length, 2);
                            System.Console.WriteLine(fpNum + " : " + Stop.watch.Elapsed);
                        }
                    }
                }

                File.Copy(workingPath + @"\Db\Uk.txt", workingPath + @"\Smi\Uk.txt");

                System.Console.WriteLine("ConvertPafData end: " + Stop.watch.Elapsed);
                return true;
            }
            catch (System.Exception)
            {
                return false;                
            }
        }

        public async Task<bool> Compile()
        {
            System.Console.WriteLine("Compile start: " + Stop.watch.Elapsed);
            try
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();

                tasks.Add("3.0", Task.Run(() =>
                {
                    Directory.CreateDirectory(workingPath + @"\3.0");

                    System.Console.WriteLine("File copy: " + Stop.watch.Elapsed);
                    List<string> smiFiles = new List<string> {@"UK_RM_CM.xml", @"UK_RM_CM_Patterns.xml", @"UK_RM_CM_Patterns.exml", @"UK_RM_CM_Settings.xml", @"UK_RM_CM.lcs", @"BFPO.txt", @"UK.txt", @"Country.txt", @"County.txt", @"PostTown.txt", @"StreetDescriptor.txt", @"StreetName.txt", @"PoBoxName.txt", @"SubBuildingDesignator.txt", @"OrganizationName.txt", @"Country_Alias.txt", @"UK_IgnorableWordsTable.txt", @"UK_WordMatchTable.txt"};
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\Smi\" + file, workingPath + @"\3.0\" + file);
                    }
                    System.Console.WriteLine("File copy end: " + Stop.watch.Elapsed);

                    Process directoryDataCompiler = RunProc(Directory.GetCurrentDirectory() + @"\DirectoryDataCompiler.exe", @"--definition " + workingPath + @"\3.0\UK_RM_CM.xml --patterns " + workingPath + @"\3.0\UK_RM_CM_Patterns.xml --password M0ntyPyth0n --licensed");
                    using (StreamReader sr = directoryDataCompiler.StandardOutput)
                    {
                        string line;
                        int linesRead;
                        Regex regex = new Regex(@"\d\d\d\d\d");
                        while ((line = sr.ReadLine()) != null)
                        {
                            Match match = regex.Match(line);

                            if (match.Success == true)
                            {
                                linesRead = int.Parse(match.Value);
                                if (linesRead % 5000 == 0)
                                {
                                    System.Console.WriteLine("3.0: " + linesRead + " : " + Stop.watch.Elapsed);
                                }
                            }
                        }
                    }
                }));
                tasks.Add("1.9", Task.Run(() =>
                {
                    Directory.CreateDirectory(workingPath + @"\1.9");

                    List<string> smiFiles = new List<string> {@"UK_RM_CM.xml", @"UK_RM_CM_Patterns.xml", @"UK_RM_CM_Patterns.exml", @"UK_RM_CM_Settings.xml", @"UK_RM_CM.lcs", @"BFPO.txt", @"UK.txt", @"Country.txt", @"County.txt", @"PostTown.txt", @"StreetDescriptor.txt", @"StreetName.txt", @"PoBoxName.txt", @"SubBuildingDesignator.txt", @"OrganizationName.txt", @"Country_Alias.txt", @"UK_IgnorableWordsTable.txt", @"UK_WordMatchTable.txt"};
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\Smi\" + file, workingPath + @"\1.9\" + file);
                    }

                    Process directoryDataCompiler = RunProc(Directory.GetCurrentDirectory() + @"\DirectoryDataCompiler.exe", @"--definition " + workingPath + @"\1.9\UK_RM_CM.xml --patterns " + workingPath + @"\1.9\UK_RM_CM_Patterns.xml --password M0ntyPyth0n --licensed");
                    using (StreamReader sr = directoryDataCompiler.StandardOutput)
                    {
                        string line;
                        int linesRead;
                        Regex regex = new Regex(@"\d\d\d\d\d");
                        while ((line = sr.ReadLine()) != null)
                        {
                            Match match = regex.Match(line);

                            if (match.Success == true)
                            {
                                linesRead = int.Parse(match.Value);
                                if (linesRead % 5000 == 0)
                                {
                                    System.Console.WriteLine("1.9: " + linesRead);
                                }
                            }
                        }
                    }
                }));

                await Task.WhenAll(tasks.Values);
                System.Console.WriteLine("Compile end: " + Stop.watch.Elapsed);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    
        public async Task<bool> Output()
        {
            System.Console.WriteLine("Output start: " + Stop.watch.Elapsed);
            try
            {
                Dictionary<string, Task> tasks = new Dictionary<string, Task>();

                tasks.Add("3.0", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\3.0");
                    Directory.CreateDirectory(outputPath + @"\3.0\UK_RM_CM");

                    System.Console.WriteLine("File copy: " + Stop.watch.Elapsed);
                    List<string> smiFiles = new List<string> {@"UK_IgnorableWordsTable.txt", @"UK_RM_CM_Patterns.exml", @"UK_WordMatchTable.txt", @"UK_RM_CM.lcs", @"UK_RM_CM.smi"};
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\3.0\" + file, outputPath + @"\3.0\UK_RM_CM\" + file);
                    }
                    File.Copy(workingPath + @"\3.0\UK_RM_CM_Settings.xml", outputPath + @"\3.0\UK_RM_CM_Settings.xml");
                    System.Console.WriteLine("File copy: " + Stop.watch.Elapsed);
                }));
                tasks.Add("1.9", Task.Run(() =>
                {
                    Directory.CreateDirectory(outputPath + @"\1.9");
                    Directory.CreateDirectory(outputPath + @"\1.9\UK_RM_CM");

                    List<string> smiFiles = new List<string> {@"UK_IgnorableWordsTable.txt", @"UK_RM_CM_Patterns.exml", @"UK_WordMatchTable.txt", @"UK_RM_CM.lcs", @"UK_RM_CM.smi"};
                    foreach (var file in smiFiles)
                    {
                        File.Copy(workingPath + @"\1.9\" + file, outputPath + @"\1.9\UK_RM_CM\" + file);
                    }
                    File.Copy(workingPath + @"\1.9\UK_RM_CM_Settings.xml", outputPath + @"\1.9\UK_RM_CM_Settings.xml");
                }));
                
                await Task.WhenAll(tasks.Values);

                System.Console.WriteLine("Output end: " + Stop.watch.Elapsed);
                return true;
            }
            catch (System.Exception)
            {
                return false;               
            }
        }
    }
}
