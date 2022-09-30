using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Common.Data;

namespace Tester;

public class SmartTester
{
    public Settings Settings { get; set; } = new Settings { Name = "SmartMatch" };
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }

    private readonly ILogger<SmartTester> logger;
    private readonly IConfiguration config;

    public SmartTester(ILogger<SmartTester> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            logger.LogInformation("Starting Tester");
            Status = ComponentStatus.InProgress;
            ChangeProgress(0, reset: true);

            Settings.Validate(config);

            CheckDisc();
            await InstallDirectory();
            await CheckLicense();
            AddLicense();
            await InjectImages();

            logger.LogInformation("Test Complete");
            Status = ComponentStatus.Ready;
            SocketConnection.SendMessage();
        }
        catch (Exception e)
        {
            Status = ComponentStatus.Error;
            SocketConnection.SendMessage();
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

        SocketConnection.SendMessage();
    }

    private void CheckDisc()
    {
        ChangeProgress(1);

        List<string> smFiles = new()
        {
            "DPV.zip",
            "LACS.zip",
            "SUITE.zip",
            "Zip4.zip"
        };

        string missingFiles = "";

        foreach (string file in smFiles)
        {
            if (!File.Exists(Path.Combine(Settings.DiscDrivePath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception("Missing files (may have disc in wrong drive): " + missingFiles);
        }
    }

    private async Task InstallDirectory()
    {
        ChangeProgress(1);

        // Stop RAFMaster
        await Utils.StopService("RAFArgosyMaster");

        // Extract zipped files to temp folder (not all files are copied from DVD)
        string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "Temp", Settings.Name);
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }

        Directory.CreateDirectory(Path.Combine(tempFolder, "DPV"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "Suite"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "Zip4"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "LACS"));

        // Make sure these are here, in case of 1st install
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match", "DPV"));
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match", "LACSLink"));
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match", "SuiteLink"));
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match", "Zip4"));

        ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "Zip4.zip"), Path.Combine(tempFolder, "Zip4"));
        ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "SUITE.zip"), Path.Combine(tempFolder, "Suite"));
        ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "DPV.zip"), Path.Combine(tempFolder, "DPV"));
        ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "LACS.zip"), Path.Combine(tempFolder, "LACS"));

        // Copy Files to Argosy
        List<string> dpvFiles = new()
        {
            "dph.hsa",
            "dph.hsc",
            "dph.hsf",
            "dvdhdr01.dat",
            "lcd",
            "live.txt",
            "llk.hsa",
            "month.dat"
        };
        foreach (string file in dpvFiles)
        {
            File.Copy(Path.Combine(tempFolder, "DPV", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match\DPV\" + file, true);
        }

        List<string> lacsFiles = new()
        {
            "live.txt",
            "llk.hs1",
            "llk.hs2",
            "llk.hs3",
            "llk.hs4",
            "llk.hs5",
            "llk.hs6",
            "llk.hsl",
            "llkhdr01.dat",
            "llk_hint.lst",
            "llk_leftrite.txt",
            "llk_strname.txt",
            "llk_urbx.lst",
            "llk_x11",
        };
        foreach (string file in lacsFiles)
        {
            File.Copy(Path.Combine(tempFolder, "LACS", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match\LACSLink\" + file, true);
        }

        List<string> suiteFiles = new()
        {
            "lcd",
            "live.txt",
            "slk.dat",
            "slkhdr01.dat",
            "slknine.lst",
            "slknoise.lst",
            "slknormal.lst",
            "slksecnums.dat",
        };
        foreach (string file in suiteFiles)
        {
            File.Copy(Path.Combine(tempFolder, "Suite", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match\SuiteLink\" + file, true);
        }

        List<string> zip4Files = new()
        {
            "0.xtl",
            "200.xtl",
            "201.xtl",
            "202.xtl",
            "203.xtl",
            "204.xtl",
            "206.xtl",
            "207.xtl",
            "208.xtl",
            "209.xtl",
            "210.xtl",
            "211.xtl",
            "212.xtl",
            "213.xtl",
            "51.xtl",
            "55.xtl",
            "56.xtl",
            "argosymonthly.lcs",
            "liven2.txt",
            "smsdkmonthly.lcs",
            "xtl-id.txt",
            "zip4crcs.txt",
        };
        foreach (string file in zip4Files)
        {
            File.Copy(Path.Combine(tempFolder, "Zip4", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match\Zip4\" + file, true);
        }

        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
    }

    private async Task CheckLicense()
    {
        ChangeProgress(1);

        // Very annoying constructor instead of DateTime.Now because DateTime.Compare doesn't work as one would expect later....
        DateTime checkTime = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);

        // Do this after creating checkTime to make sure timing is right, start the service to populate the txt, stop to release control so StreamReader can access
        await Utils.StartService("RAFArgosyMaster");
        await Utils.StopService("RAFArgosyMaster");

        using StreamReader sr = new(@"C:\ProgramData\RAF\ArgosyPost\Log\Master_latest.txt");

        bool dongleCheckPass = false;
        Regex match = new(@"(Argosy Post Dongle )(\d\d-\d\d\d\d\d\d\d\d)");
        string line;

        while ((line = sr.ReadLine()) != null)
        {
            Match dongleFound = match.Match(line);

            if (dongleFound.Success)
            {
                string logDongleId = dongleFound.Groups[2].Value;

                if (logDongleId != Settings.DongleId)
                {
                    throw new Exception("Dongle ID in log does not match dongle to be tested against");
                }
            }

            if (line.Contains("SM Adaptor       Error initializing license subsystem:"))
            {
                int logMonth = int.Parse(line[..2]);
                int logDay = int.Parse(line.Substring(3, 2));
                int logYear = int.Parse(string.Concat("20", line.AsSpan(6, 2)));

                int logHour = int.Parse(line.Substring(9, 2));
                int logMinute = int.Parse(line.Substring(12, 2));
                int logSecond = int.Parse(line.Substring(15, 2));

                DateTime logTime = new(logYear, logMonth, logDay, logHour, logMinute, logSecond, 0);

                int dateCompare = DateTime.Compare(logTime, checkTime);

                if (dateCompare >= 0)
                {
                    dongleCheckPass = true;
                    break;
                }
            }
        }

        if (!dongleCheckPass)
        {
            throw new Exception("Directory did not pass LCS test");
        }
    }

    private void AddLicense()
    {
        // Cleanup
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.txt")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.txt"));
        }

        // Read and regex match to directory year and month based on created date
        Regex match = new(@"(Created: )(\d\d\d\d-\d\d-\d\d)+");
        string dataYearMonth = "";

        using (StreamReader sr = new(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match\Zip4\xtl-id.txt"))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Match dataYearMonthFound = match.Match(line);

                if (dataYearMonthFound.Success)
                {
                    dataYearMonth = dataYearMonthFound.Groups[2].Value;
                    break;
                }
            }
        }

        // Check that directory year and month were found
        if (string.IsNullOrEmpty(dataYearMonth))
        {
            throw new Exception("Cannot find directory data year/month, nedded to add dongle to LCS file");
        }

        // Set dataYearMonth to year and month (day is always the 1st)
        dataYearMonth = string.Concat(dataYearMonth.AsSpan(0, 4), dataYearMonth.AsSpan(5, 2), "01");

        // Create txt version of ArgosyMonthly that includes dongle
        using (StreamWriter sw = new(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.txt"), true))
        {
            sw.WriteLine("Date=" + dataYearMonth);
            sw.WriteLine("Dongles:");
            sw.WriteLine(Settings.DongleId);
        }

        // Encrypt new Uk dongle list, but first wrap the combined paths in quotes to get around spaced directories
        string encryptRepFileName = Path.Combine(Directory.GetCurrentDirectory(), "EncryptREP.exe");
        const string encryptRepArgs = "-x lcs argosymonthly.txt";

        // Remember to add EncryptREP to Windows Defender exclusion list
        Process encryptRep = Utils.RunProc(encryptRepFileName, encryptRepArgs);
        encryptRep.WaitForExit();

        // Check that LCS file was actually created
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.lcs")))
        {
            throw new Exception("LCS file was not created, something likely wrong with EncryptREP");
        }

        // Overwrite old LCS, start RAFArgosyMaster
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.lcs"), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match\Zip4\argosymonthly.lcs", true);

        // Cleanup
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.txt")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.txt"));
        }
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.lcs")))
        {
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "argosymonthly.lcs"));
        }
    }

    private async Task InjectImages()
    {
        ChangeProgress(1);

        await Utils.StartService("RAFArgosyMaster");

        // Set config with ControlPort, wait for Recmodule to initialize after setting
        Process controlPort = Utils.RunProc(@"C:\Users\User\Desktop\Control_Example.exe", "7 localhost 1069 test01");
        controlPort.WaitForExit();
        await Task.Delay(TimeSpan.FromSeconds(15));

        BackEndSockC beSockC = new("172.27.23.57");
        CancellationTokenSource stoppingTokenSource = new();

        _ = Task.Run(async () =>
        {
            // Wait for BE connection initialize
            await Task.Delay(TimeSpan.FromSeconds(15));

            // Inject known good test deck
            Process feSockC = Utils.RunProc(@"C:\Program Files\Argosy Post\FE_SOCK_C_Test.exe", @"-f C:\Users\User\Desktop\OHURB\*.tif");
            feSockC.WaitForExit();

            // Wait for inject to finish through AP
            await Task.Delay(TimeSpan.FromSeconds(15));
            stoppingTokenSource.Cancel();
        });

        await beSockC.ExecuteAsync(stoppingTokenSource);

        if (beSockC.FinalCount < 5)
        {
            throw new Exception("Directory did not pass injection test");
        }

        ChangeProgress(10);
    }
}