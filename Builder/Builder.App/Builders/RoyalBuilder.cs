using System.Diagnostics;
using FlaUI.Core;
using FlaUI.UIA2;
using FlaUI.Core.AutomationElements;
using System.Text.RegularExpressions;
using System.Xml;
using Builder.App.Utils;
using System.Linq;

namespace Builder.App.Builders;

public class RoyalBuilder
{
    private readonly string inputPath;
    private readonly string workingPath;
    private readonly string outputPath;
    private readonly string key;
    private readonly Settings settings;
    private readonly DatabaseContext context;
    private readonly Action<int> progress;
    private string year;
    private string month;

    public RoyalBuilder(Settings settings, DatabaseContext context, Action<int> progress)
    {
        this.inputPath = settings.AddressDataPath;
        this.workingPath = settings.WorkingPath;
        this.outputPath = settings.OutputPath;
        this.key = settings.Key;
        this.settings = settings;
        this.context = context;
        this.progress = progress;

        this.month = settings.DataMonth;
        this.year = settings.DataYear;
    }

    public async Task Extract()
    {
        PafKey key = context.PafKeys.Where(x => (int.Parse(settings.DataMonth) == x.DataMonth) && (int.Parse(settings.DataYear) == x.DataYear)).FirstOrDefault();

        if (key == null)
        {
            throw new Exception("Key not found in db");
        }

        progress(1);

        using (UIA2Automation automation = new UIA2Automation())
        {
            Application app = FlaUI.Core.Application.Launch(Path.Combine(inputPath, @"SetupRM.exe"));
            // Annoyingly have to do this because SetupRM is not created correctly, "splash screen" effect causes FlaUI to grab the window before the body is populated with elements
            await Task.Delay(TimeSpan.FromSeconds(3));
            Window[] windows = app.GetAllTopLevelWindows(automation);

            // Check that main window elements can be found
            AutomationElement keyText = windows[0].FindFirstDescendant(cf => cf.ByClassName("TEdit"));
            if (keyText == null)
            {
                throw new Exception("Could not find the window elements");
            }
            // TODO: Somehow if key is wrong, look for label maybe?
            keyText.AsTextBox().Enter(key.Value);

            // 1st page
            AutomationElement beginButton = windows[0].FindFirstDescendant(cf => cf.ByClassName("TButton"));
            beginButton.AsButton().Click();
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 2nd page
            AutomationElement nextButton = windows[0].FindFirstDescendant(cf => cf.ByClassName("TButton"));
            nextButton.AsButton().Click();
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 3rd page
            AutomationElement extractText = windows[0].FindFirstDescendant(cf => cf.ByClassName("TEdit"));
            extractText.AsTextBox().Enter(inputPath);
            AutomationElement startButton = windows[0].FindFirstDescendant(cf => cf.ByClassName("TButton"));
            startButton.AsButton().Click();

            await WaitForExtract(windows);

            windows[0].Close();
        }

        progress(21);
    }

    public void Cleanup(bool fullClean)
    {
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

            progress(1);

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

        progress(1);
    }

    public void UpdateSmiFiles()
    {
        Directory.CreateDirectory(Path.Combine(workingPath, @"Smi"));

        // Process smiCheckout = Utils.Utils.RunProc(@"C:\Program Files\TortoiseSVN\bin\svn.exe", @"export https://scm.raf.com/repos/tags/TechServices/Tag24-UK_RM_CM-3.0/Directory_Creation_Files --username billym " + Path.Combine(workingPath, @"Smi") + " --force");
        // smiCheckout.WaitForExit();

        // Process dongleCheckout = Utils.Utils.RunProc(@"C:\Program Files\TortoiseSVN\bin\svn.exe", @"export https://scm.raf.com/repos/trunk/TechServices/SMI/Directories/UK/DongleList --username billym " + Path.Combine(workingPath, @"Smi") + " --force");
        // dongleCheckout.WaitForExit();

        Utils.Utils.CopyFiles(@"C:\Users\billy\Desktop\Testing\DirectoryCreationFiles", Path.Combine(workingPath, @"Smi"));

        // Edit SMi definition xml file with updated date 
        XmlDocument defintionFile = new XmlDocument();
        defintionFile.Load(Path.Combine(workingPath, @"Smi", @"UK_RM_CM.xml"));
        XmlNode root = defintionFile.DocumentElement;
        root.Attributes[1].Value = @"Y" + year + @"M" + month;
        defintionFile.Save(Path.Combine(workingPath, @"Smi", @"UK_RM_CM.xml"));

        // Edit Uk dongle list with updated date
        using (StreamWriter sw = new StreamWriter(Path.Combine(workingPath, @"Smi", @"DongleTemp.txt")))
        {
            sw.WriteLine(@"Date=" + year + month + @"19");

            using (StreamReader sr = new StreamReader(Path.Combine(workingPath, @"Smi", @"UK_RM_CM.txt")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    sw.WriteLine(line);
                }
            }
        }

        File.Delete(Path.Combine(workingPath, @"Smi", @"UK_RM_CM.txt"));
        File.Delete(Path.Combine(workingPath, @"Smi", @"UK_RM_CM.lcs"));
        File.Delete(Path.Combine(workingPath, @"Smi", @"UK_RM_CM_Patterns.exml"));
        
        File.Move(Path.Combine(workingPath, @"Smi", @"DongleTemp.txt"), Path.Combine(workingPath, @"Smi", @"UK_RM_CM.txt"));

        // Encrypt new Uk dongle list, but first wrap the combined paths in quotes to get around spaced directories
        string encryptRepFileName = Utils.Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "EncryptREP.exe"));
        string encryptRepArgs = @"-x lcs " + Utils.Utils.WrapQuotes(Path.Combine(workingPath, "Smi", "UK_RM_CM.txt"));

        Process encryptRep = Utils.Utils.RunProc(encryptRepFileName, encryptRepArgs);
        encryptRep.WaitForExit();

        // Encrypt patterns, but first wrap the combined paths in quotes to get around spaced directories
        string encryptPatternsFileName = Utils.Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "EncryptPatterns.exe"));
        string encryptPatternsArgs = @"--patterns " + Utils.Utils.WrapQuotes(Path.Combine(workingPath, "Smi", "UK_RM_CM_Patterns.xml")) + @" --clickCharge";
        
        Process encryptPatterns = Utils.Utils.RunProc(encryptPatternsFileName, encryptPatternsArgs);
        encryptPatterns.WaitForExit();

        // If this file wasn't created then EncryptPatterns silently failed, likeliest cause is missing a redistributable
        if (!File.Exists(Path.Combine(workingPath, "Smi", "UK_RM_CM_Patterns.exml")))
        {
            throw new Exception("Missing C++ 2010 x86 redistributable, EncryptPatterns and DirectoryDataCompiler 1.9 won't work. Also check that SQL CE is installed for 1.9");
        }

        progress(1);
    }

    public void ConvertPafData()
    {
        // Move address data files to working folder "Db"
        Utils.Utils.CopyFiles(Path.Combine(inputPath, "PAF COMPRESSED STD"), Path.Combine(workingPath, "Db"));
        Utils.Utils.CopyFiles(Path.Combine(inputPath, "ALIAS"), Path.Combine(workingPath, "Db"));

        // Start ConvertPafData tool, listen for output
        string convertPafDataFileName = Utils.Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "ConvertPafData.exe"));
        string convertPafDataArgs = @"--pafPath " + Utils.Utils.WrapQuotes(Path.Combine(workingPath, "Db")) + @" --lastPafFileNum 15";

        Process convertPafData = Utils.Utils.RunProc(convertPafDataFileName, convertPafDataArgs);
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
                    // logger.LogInformation("ConvertPafData processing file: " + matchFound.Value);
                }
            }
        }

        // Copy CovertPafData finished result to SMi build files folder
        File.Copy(Path.Combine(workingPath, "Db", "Uk.txt"), Path.Combine(workingPath, "Smi", "Uk.txt"), true);

        progress(23);
    }

    public async Task Compile()
    {
        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("3.0", Task.Run(() => CompileRunner("3.0")));
        tasks.Add("1.9", Task.Run(() => CompileRunner("1.9")));

        await Task.WhenAll(tasks.Values);

        progress(50);
    }

    public async Task Output()
    {
        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("3.0", Task.Run(() => OutputRunner("3.0")));
        tasks.Add("1.9", Task.Run(() => OutputRunner("1.9")));

        await Task.WhenAll(tasks.Values);

        progress(1);
    }

    public void CheckBuildComplete()
    {
        RoyalBundle bundle = context.RoyalBundles.Where(x => (int.Parse(month) == x.DataMonth) && (int.Parse(year) == x.DataYear)).FirstOrDefault();
        bundle.IsBuildComplete = true;

        context.SaveChanges();
    }

    private async Task WaitForExtract(Window[] windows)
    {
        AutomationElement progressbar = windows[0].FindFirstDescendant(cf => cf.ByClassName("TProgressBar"));

        if (progressbar == null)
        {
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(30));
        await WaitForExtract(windows);
    }

    private void CompileRunner(string version)
    {
        Directory.CreateDirectory(workingPath + @"\" + version);

        List<string> smiFiles = new List<string> { @"UK_RM_CM.xml", @"UK_RM_CM_Patterns.xml", @"UK_RM_CM_Patterns.exml", @"UK_RM_CM_Settings.xml", @"UK_RM_CM.lcs", @"BFPO.txt", @"UK.txt", @"Country.txt", @"County.txt", @"PostTown.txt", @"StreetDescriptor.txt", @"StreetName.txt", @"PoBoxName.txt", @"SubBuildingDesignator.txt", @"OrganizationName.txt", @"Country_Alias.txt", @"UK_IgnorableWordsTable.txt", @"UK_WordMatchTable.txt" };
        foreach (string file in smiFiles)
        {
            File.Copy(workingPath + @"\Smi\" + file, workingPath + @"\" + version + @"\" + file, true);
        }

        string directoryDataCompilerFileName = Utils.Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", version, "DirectoryDataCompiler.exe"));
        string directoryDataCompilerArgs = @"--definition " + Utils.Utils.WrapQuotes(Path.Combine(workingPath, version, "UK_RM_CM.xml")) + @" --patterns " + Utils.Utils.WrapQuotes(Path.Combine(workingPath, version, "UK_RM_CM_Patterns.xml")) + @" --password M0ntyPyth0n --licensed";

        Process directoryDataCompiler = Utils.Utils.RunProc(directoryDataCompilerFileName, directoryDataCompilerArgs);
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
                        // logger.LogInformation("DirectoryDataCompiler " + version + " addresses processed: " + matchFound.Value);
                    }
                }
            }
        }
    }

    private void OutputRunner(string version)
    {
        Directory.CreateDirectory(Path.Combine(outputPath, version, "UK_RM_CM"));

        List<string> smiFiles = new List<string> { @"UK_IgnorableWordsTable.txt", @"UK_RM_CM_Patterns.exml", @"UK_WordMatchTable.txt", @"UK_RM_CM.lcs", @"UK_RM_CM.smi" };
        foreach (string file in smiFiles)
        {
            File.Copy(Path.Combine(workingPath, version, file), Path.Combine(outputPath, version, "UK_RM_CM", file), true);
        }
        File.Copy(Path.Combine(workingPath, version, "UK_RM_CM_Settings.xml"), Path.Combine(outputPath, version, "UK_RM_CM_Settings.xml"), true);
    }
}
