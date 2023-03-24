using System.Diagnostics;
using FlaUI.Core;
using FlaUI.UIA2;
using FlaUI.Core.AutomationElements;
using System.Text.RegularExpressions;
using System.Xml;
using Common.Data;

namespace Builder;

public class RoyalBuilder
{
    public Settings Settings { get; set; } = new Settings { Name = "RoyalMail" };
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }
    public Action<DirectoryType, DatabaseContext> SendMessage { get; set; }

    private readonly ILogger<RoyalBuilder> logger;
    private readonly IConfiguration config;
    private readonly DatabaseContext context;

    private string DataMonth;
    private string DataYear;

    public RoyalBuilder(ILogger<RoyalBuilder> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.context = context;
    }

    public async Task ExecuteAsyncAuto(CancellationToken stoppingToken)
    {
        SendMessage(DirectoryType.RoyalMail, context);

        if (!Settings.AutoBuildEnabled)
        {
            logger.LogDebug("AutoBuild disabled");
            return;
        }
        if (Status != ComponentStatus.Ready)
        {
            logger.LogInformation("Build already in progress");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Builder - Auto mode");

                TimeSpan waitTime = Settings.CalculateWaitTime(logger, Settings);
                await Task.Delay(TimeSpan.FromSeconds(waitTime.TotalSeconds), stoppingToken);

                foreach (RoyalBundle bundle in context.RoyalBundles.Where(x => x.IsReadyForBuild && !x.IsBuildComplete).ToList())
                {
                    // await ExecuteAsync(bundle.DataYearMonth);
                }
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug("{Message}", e.Message);
        }
        catch (Exception e)
        {
            logger.LogError("{Message}", e.Message);
        }
    }

    public async Task ExecuteAsync(string DataYearMonth, string RoyalKey)
    {
        try
        {
            logger.LogInformation("Starting Builder");
            Status = ComponentStatus.InProgress;
            ChangeProgress(0, reset: true);

            DataYear = DataYearMonth[..4];
            DataMonth = DataYearMonth.Substring(4, 2);
            Settings.Validate(config, DataYearMonth);

            CheckKey(DataYearMonth, RoyalKey);
            await Extract();
            Cleanup(fullClean: true);
            UpdateSmiFiles();
            ConvertPafData();
            await Compile();
            await Output();
            Cleanup(fullClean: false);
            CheckBuildComplete();

            logger.LogInformation("Build Complete: {DataYearMonth}", DataYearMonth);
            Status = ComponentStatus.Ready;
            SendMessage(DirectoryType.RoyalMail, context);
        }
        catch (TaskCanceledException e)
        {
            Status = ComponentStatus.Ready;
            SendMessage(DirectoryType.RoyalMail, context);
            logger.LogDebug("{Message}", e.Message);
        }
        catch (Exception e)
        {
            Status = ComponentStatus.Error;
            SendMessage(DirectoryType.RoyalMail, context);
            logger.LogError("{Message}", e.Message);
        }
    }

    public void ChangeProgress(int changeAmount, bool reset = false)
    {
        if (reset)
        {
            Progress = 0;
        }
        else
        {
            Progress += changeAmount;
        }

        SendMessage(DirectoryType.RoyalMail, context);
    }

    private void CheckKey(string DataYearMonth, string royalKey)
    {
        Regex regex = new("(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)");
        Match match = regex.Match(royalKey);

        if (match == null)
        {
            throw new Exception("Key could not be found in email body");
        }

        PafKey filteredKey = new()
        {
            DataYear = int.Parse(DataYearMonth[..4]),
            DataMonth = int.Parse(DataYearMonth.Substring(4, 2)),
            Value = match.Groups[1].Value + match.Groups[3].Value + match.Groups[5].Value + match.Groups[7].Value + match.Groups[9].Value + match.Groups[11].Value + match.Groups[13].Value + match.Groups[15].Value,
        };

        bool keyInDb = context.PafKeys.Any(x => filteredKey.Value == x.Value);

        if (!keyInDb)
        {
            logger.LogInformation("Unique PafKey added: {DataMonth}/{DataYear}", filteredKey.DataMonth, filteredKey.DataYear);
            context.PafKeys.Add(filteredKey);
            context.SaveChanges();
        }
    }

    private async Task Extract()
    {
        PafKey key = context.PafKeys.Where(x => (int.Parse(DataMonth) == x.DataMonth) && (int.Parse(DataYear) == x.DataYear)).FirstOrDefault();

        if (key == null)
        {
            throw new Exception("Key not found in db");
        }

        ChangeProgress(1);

        using (UIA2Automation automation = new())
        {
            Application app = FlaUI.Core.Application.Launch(Path.Combine(Settings.AddressDataPath, "SetupRM.exe"));
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
            windows[0].SetForeground();
            beginButton.AsButton().Click();
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 2nd page
            AutomationElement nextButton = windows[0].FindFirstDescendant(cf => cf.ByClassName("TButton"));
            windows[0].SetForeground();
            nextButton.AsButton().Click();
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 3rd page
            AutomationElement extractText = windows[0].FindFirstDescendant(cf => cf.ByClassName("TEdit"));
            extractText.AsTextBox().Enter(Settings.AddressDataPath);
            AutomationElement startButton = windows[0].FindFirstDescendant(cf => cf.ByClassName("TButton"));
            windows[0].SetForeground();
            startButton.AsButton().Click();
            // Annoying have to wait because SetupRM hangs before moving to extract causing the WaitForExtract to miss the TProgressBar element is needs to watch
            await Task.Delay(TimeSpan.FromSeconds(3));

            await WaitForExtract(windows);

            windows[0].Close();
        }

        ChangeProgress(21);
    }

    private void Cleanup(bool fullClean)
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
        Directory.CreateDirectory(Settings.WorkingPath);
        Directory.CreateDirectory(Settings.OutputPath);

        DirectoryInfo wp = new(Settings.WorkingPath);
        DirectoryInfo op = new(Settings.OutputPath);

        if (!fullClean)
        {
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

            ChangeProgress(1);

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

        ChangeProgress(1);
    }

    private void UpdateSmiFiles()
    {
        Directory.CreateDirectory(Path.Combine(Settings.WorkingPath, "Smi"));

        // Process smiCheckout = Utils.RunProc(@"C:\Program Files\TortoiseSVN\bin\svn.exe", @"export https://scm.raf.com/repos/tags/TechServices/Tag24-UK_RM_CM-3.0/Directory_Creation_Files --username billym " + Path.Combine(Settings.WorkingPath, @"Smi") + " --force");
        // smiCheckout.WaitForExit();

        // Process dongleCheckout = Utils.RunProc(@"C:\Program Files\TortoiseSVN\bin\svn.exe", @"export https://scm.raf.com/repos/trunk/TechServices/SMI/Directories/UK/DongleList --username billym " + Path.Combine(Settings.WorkingPath, @"Smi") + " --force");
        // dongleCheckout.WaitForExit();

        Utils.CopyFiles(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "DirectoryCreationFiles"), Path.Combine(Settings.WorkingPath, "Smi"));

        // Edit SMi definition xml file with updated date 
        XmlDocument defintionFile = new();
        defintionFile.Load(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.xml"));
        XmlNode root = defintionFile.DocumentElement;
        root.Attributes[1].Value = "Y" + DataYear + "M" + DataMonth;
        defintionFile.Save(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.xml"));

        // Edit Uk dongle list with updated date
        using (StreamWriter sw = new(Path.Combine(Settings.WorkingPath, "Smi", "DongleTemp.txt"), true, System.Text.Encoding.Unicode))
        {
            sw.WriteLine("Date=" + DataYear + DataMonth + "19");

            using StreamReader sr = new(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.txt"), System.Text.Encoding.Unicode);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                sw.WriteLine(line);
            }
        }

        File.Delete(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.txt"));
        File.Delete(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.lcs"));
        File.Delete(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM_Patterns.exml"));

        File.Move(Path.Combine(Settings.WorkingPath, "Smi", "DongleTemp.txt"), Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.txt"));

        // Encrypt new Uk dongle list, but first wrap the combined paths in quotes to get around spaced directories
        string encryptRepFileName = Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "EncryptREP.exe"));
        string encryptRepArgs = "-x lcs " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM.txt"));

        Process encryptRep = Utils.RunProc(encryptRepFileName, encryptRepArgs);
        encryptRep.WaitForExit();

        // Encrypt patterns, but first wrap the combined paths in quotes to get around spaced directories
        string encryptPatternsFileName = Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "EncryptPatterns.exe"));
        string encryptPatternsArgs = "--patterns " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM_Patterns.xml")) + " --clickCharge";

        Process encryptPatterns = Utils.RunProc(encryptPatternsFileName, encryptPatternsArgs);
        encryptPatterns.WaitForExit();

        // If this file wasn't created then EncryptPatterns silently failed, likeliest cause is missing a redistributable
        if (!File.Exists(Path.Combine(Settings.WorkingPath, "Smi", "UK_RM_CM_Patterns.exml")))
        {
            throw new Exception("Missing C++ 2010 x86 redistributable, EncryptPatterns and DirectoryDataCompiler 1.9 won't work. Also check that SQL CE is installed for 1.9");
        }

        ChangeProgress(1);
    }

    private void ConvertPafData()
    {
        // Move address data files to working folder "Db"
        Utils.CopyFiles(Path.Combine(Settings.AddressDataPath, "PAF COMPRESSED STD"), Path.Combine(Settings.WorkingPath, "Db"));
        Utils.CopyFiles(Path.Combine(Settings.AddressDataPath, "ALIAS"), Path.Combine(Settings.WorkingPath, "Db"));

        // Start ConvertPafData tool, listen for output
        string convertPafDataFileName = Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "ConvertPafData.exe"));
        string convertPafDataArgs = "--pafPath " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, "Db")) + " --lastPafFileNum 15";

        Process convertPafData = Utils.RunProc(convertPafDataFileName, convertPafDataArgs);
        using (StreamReader sr = convertPafData.StandardOutput)
        {
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
                    logger.LogDebug("ConvertPafData processing file: {Value}", matchFound.Value);
                }
            }
        }

        // Copy CovertPafData finished result to SMi build files folder
        File.Copy(Path.Combine(Settings.WorkingPath, "Db", "Uk.txt"), Path.Combine(Settings.WorkingPath, "Smi", "Uk.txt"), true);

        ChangeProgress(23);
    }

    private async Task Compile()
    {
        Dictionary<string, Task> tasks = new()
        {
            { "3.0", Task.Run(() => CompileRunner("3.0")) },
            { "1.9", Task.Run(() => CompileRunner("1.9")) }
        };

        await Task.WhenAll(tasks.Values);

        ChangeProgress(50);
    }

    private async Task Output()
    {
        Dictionary<string, Task> tasks = new()
        {
            { "3.0", Task.Run(() => OutputRunner("3.0")) },
            { "1.9", Task.Run(() => OutputRunner("1.9")) }
        };

        await Task.WhenAll(tasks.Values);

        ChangeProgress(1);
    }

    private void CheckBuildComplete()
    {
        RoyalBundle bundle = context.RoyalBundles.Where(x => (int.Parse(DataMonth) == x.DataMonth) && (int.Parse(DataYear) == x.DataYear)).FirstOrDefault();
        bundle.IsBuildComplete = true;

        DateTime timestamp = DateTime.Now;
        string hour;
        string minute;
        string ampm;
        if (timestamp.Minute < 10)
        {
            minute = timestamp.Minute.ToString().PadLeft(2, '0');
        }
        else
        {
            minute = timestamp.Minute.ToString();
        }
        if (timestamp.Hour > 12)
        {
            hour = (timestamp.Hour - 12).ToString();
            ampm = "pm";
        }
        else
        {
            hour = timestamp.Hour.ToString();
            ampm = "am";
        }
        bundle.CompileDate = timestamp.Month.ToString() + "/" + timestamp.Day + "/" + timestamp.Year.ToString();
        bundle.CompileTime = hour + ":" + minute + ampm;

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
        Directory.CreateDirectory(Settings.WorkingPath + @"\" + version);

        foreach (string file in new List<string> { "UK_RM_CM.xml", "UK_RM_CM_Patterns.xml", "UK_RM_CM_Patterns.exml", "UK_RM_CM_Settings.xml", "UK_RM_CM.lcs", "BFPO.txt", "UK.txt", "Country.txt", "County.txt", "PostTown.txt", "StreetDescriptor.txt", "StreetName.txt", "PoBoxName.txt", "SubBuildingDesignator.txt", "OrganizationName.txt", "Country_Alias.txt", "UK_IgnorableWordsTable.txt", "UK_WordMatchTable.txt" })
        {
            File.Copy(Settings.WorkingPath + @"\Smi\" + file, Settings.WorkingPath + @"\" + version + @"\" + file, true);
        }

        string directoryDataCompilerFileName = Utils.WrapQuotes(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", version, "DirectoryDataCompiler.exe"));
        string directoryDataCompilerArgs = "--definition " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, version, "UK_RM_CM.xml")) + " --patterns " + Utils.WrapQuotes(Path.Combine(Settings.WorkingPath, version, "UK_RM_CM_Patterns.xml")) + " --password M0ntyPyth0n --licensed";

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
                throw new Exception("Error detected in DirectoryDataCompiler " + version);
            }

            Match matchFound = match.Match(line);

            if (matchFound.Success)
            {
                linesRead = int.Parse(matchFound.Value);
                if (linesRead % 5000 == 0)
                {
                    logger.LogDebug("DirectoryDataCompiler {version} addresses processed: {Value}", version, matchFound.Value);
                }
            }
        }
    }

    private void OutputRunner(string version)
    {
        Directory.CreateDirectory(Path.Combine(Settings.OutputPath, version, "UK_RM_CM"));

        foreach (string file in new List<string> { "UK_IgnorableWordsTable.txt", "UK_RM_CM_Patterns.exml", "UK_WordMatchTable.txt", "UK_RM_CM.lcs", "UK_RM_CM.smi" })
        {
            File.Copy(Path.Combine(Settings.WorkingPath, version, file), Path.Combine(Settings.OutputPath, version, "UK_RM_CM", file), true);
        }
        File.Copy(Path.Combine(Settings.WorkingPath, version, "UK_RM_CM_Settings.xml"), Path.Combine(Settings.OutputPath, version, "UK_RM_CM_Settings.xml"), true);
    }
}