using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Server.Common;

namespace Server.Tester;

public class DirTester : BaseModule
{
    private readonly ILogger<DirTester> logger;
    private readonly IConfiguration config;

    private string discDrivePath = "";

    public DirTester(ILogger<DirTester> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }

    public async Task Start(string directoryName)
    {
        try
        {
            logger.LogInformation("Starting Tester");
            Status = ModuleStatus.InProgress;

            discDrivePath = config.GetValue<string>($"TestDrivePaths:{directoryName}");

            switch (directoryName)
            {
                case "Zip4":
                    Zip4CheckDisc();
                    break;
                case "SmartMatch":
                    SmartMatchCheckDisc();
                    await SmartMatchInstallDirectory();
                    // await CheckLicense();
                    // AddLicense();
                    // await InjectImages(directoryName);
                    break;
                case "Parascript":
                    ParascriptCheckDisc();
                    await ParascriptInstallDirectory();
                    // await InjectImages(directoryName);
                    break;
                case "RoyalMail":
                    RoyalMailCheckDisc();
                    await RoyalMailInstallDirectory();
                    // await CheckLicense();
                    // AddLicense();
                    // await InjectImages(directoryName);
                    break;
            }

            logger.LogInformation("Test Complete");
            Status = ModuleStatus.Ready;
        }
        catch (Exception e)
        {
            Status = ModuleStatus.Error;
            Message = "Check logs for more details";
            logger.LogError($"{e.Message}");
        }
    }

    private async Task InjectImages(string directoryName)
    {
        await Utils.StartService("RAFArgosyMaster");

        // Set config with ControlPort, wait for Recmodule to initialize after setting
        Process controlPort = Utils.RunProc(@"C:\Users\User\Desktop\Control_Example.exe", "7 localhost 1069 " + directoryName);
        controlPort.WaitForExit();
        await Task.Delay(TimeSpan.FromSeconds(15));

        BackEndSockC beSockC = new("172.27.23.57");
        CancellationTokenSource stoppingTokenSource = new();

        _ = Task.Run(async () =>
        {
            // Wait for AP initialize
            await Task.Delay(TimeSpan.FromSeconds(15));

            // Inject known good test deck
            string testDeckPath = Path.Combine(Directory.GetCurrentDirectory(), "TestDecks", directoryName);
            Process feSockC = Utils.RunProc(@"C:\Program Files\Argosy Post\FE_SOCK_C_Test.exe", $"-f {testDeckPath}\\*.tif");
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
    }

    private async Task CheckLicense()
    {
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

                if (logDongleId != config.GetValue<string>("DongleId"))
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

    private void Zip4CheckDisc()
    {
        List<string> smFiles = new()
        {
            "Zip4.zip"
        };

        string missingFiles = "";

        foreach (string file in smFiles)
        {
            if (!File.Exists(Path.Combine(discDrivePath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception("Missing files (may have disc in wrong drive): " + missingFiles);
        }
    }

    private void SmartMatchCheckDisc()
    {
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
            if (!File.Exists(Path.Combine(discDrivePath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception("Missing files (may have disc in wrong drive): " + missingFiles);
        }
    }

    private void ParascriptCheckDisc()
    {
        const string dpvFile = "DPV.zip";
        const string suiteFile = "SUITE.zip";
        const string zip4File = "Zip4.zip";
        List<string> lacsFiles = new()
        {
            "fileinfo_log.txt",
            "live.txt",
            "llk.hs1",
            "llk.hs2",
            "llk.hs3",
            "llk.hs4",
            "llk.hs5",
            "llk.hs6",
            "llk.hsa",
            "llk.hsl",
            "llkhdr01.dat",
            "llk_cln.dat",
            "llk_cln.txt",
            "llk_crd.dat",
            "llk_czp.dat",
            "llk_czp.txt",
            "llk_dsc.dat",
            "llk_hint.lst",
            "llk_lcd",
            "llk_leftrite.txt",
            "llk_lln.dat",
            "llk_nam.dat",
            "llk_pno.dat",
            "llk_rv9.dat",
            "llk_rv9.esd",
            "llk_rv9.idx",
            "llk_sno.dat",
            "llk_strname.txt",
            "llk_suf.dat",
            "llk_urbx.lst",
            "llk_x11"
        };

        string missingFiles = "";

        if (!File.Exists(Path.Combine(discDrivePath, "DPV", dpvFile)))
        {
            missingFiles += dpvFile + ", ";
        }
        if (!File.Exists(Path.Combine(discDrivePath, "Suite", suiteFile)))
        {
            missingFiles += suiteFile + ", ";
        }
        if (!File.Exists(Path.Combine(discDrivePath, "Zip4", zip4File)))
        {
            missingFiles += zip4File + ", ";
        }
        foreach (string file in lacsFiles)
        {
            if (!File.Exists(Path.Combine(discDrivePath, "LACS", file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception("Missing files (may have disc in wrong drive): " + missingFiles);
        }
    }

    private void RoyalMailCheckDisc()
    {
        const string rmSettingsFile = "UK_RM_CM_Settings.xml";
        List<string> rmFiles = new()
        {
            "UK_IgnorableWordsTable.txt",
            "UK_RM_CM.lcs",
            "UK_RM_CM.smi",
            "UK_RM_CM_Patterns.exml",
            "UK_WordMatchTable.txt",
        };

        string missingFiles = "";

        if (!File.Exists(Path.Combine(discDrivePath, rmSettingsFile)))
        {
            missingFiles += rmSettingsFile + ", ";
        }
        foreach (string file in rmFiles)
        {
            if (!File.Exists(Path.Combine(discDrivePath, "UK_RM_CM", file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception("Missing files (may have disc in wrong drive): " + missingFiles);
        }
    }

    private async Task SmartMatchInstallDirectory()
    {
        // Stop RAFMaster
        await Utils.StopService("RAFArgosyMaster");

        // Extract zipped files to temp folder (not all files are copied from DVD)
        string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDecks", "TesterTemp", "SmartMatch");
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

        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "Zip4.zip"), Path.Combine(tempFolder, "Zip4"));
        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "SUITE.zip"), Path.Combine(tempFolder, "Suite"));
        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "DPV.zip"), Path.Combine(tempFolder, "DPV"));
        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "LACS.zip"), Path.Combine(tempFolder, "LACS"));

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

    private async Task ParascriptInstallDirectory()
    {
        // Stop RAFMaster
        await Utils.StopService("RAFArgosyMaster");

        // Extract zipped files to temp folder (not all files are copied from DVD)
        string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestDecks", "TesterTemp", "Parascript");
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }

        Directory.CreateDirectory(Path.Combine(tempFolder, "DPV"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "Suite"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "Zip4"));
        Directory.CreateDirectory(Path.Combine(tempFolder, "LACS"));

        // Make sure these are here, in case of 1st install
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "DPV"));
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "LACSLink"));
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "SuiteLink"));
        Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "Zip4"));

        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "Zip4", "Zip4.zip"), Path.Combine(tempFolder, "Zip4"));
        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "Suite", "SUITE.zip"), Path.Combine(tempFolder, "Suite"));
        ZipFile.ExtractToDirectory(Path.Combine(discDrivePath, "DPV", "DPV.zip"), Path.Combine(tempFolder, "DPV"));
        Utils.CopyFiles(Path.Combine(discDrivePath, "LACS"), Path.Combine(tempFolder, "LACS"));

        // Copy Files to Argosy
        List<string> dpvFiles = new()
        {
            "dph.hsa",
            "dph.hsc",
            "dph.hsf",
            "dph.hsx",
            "fileinfo_log.txt",
            "lcd",
            "month.dat",
        };
        foreach (string file in dpvFiles)
        {
            File.Copy(Path.Combine(tempFolder, "DPV", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript\DPV\" + file, true);
        }

        List<string> lacsFiles = new()
        {
            "fileinfo_log.txt",
            "llk.hs1",
            "llk.hs2",
            "llk.hs3",
            "llk.hs4",
            "llk.hs5",
            "llk.hs6",
            "llk.hsa",
            "llk.hsl",
            "llkhdr01.dat",
            "llk_cln.dat",
            "llk_cln.txt",
            "llk_crd.dat",
            "llk_czp.dat",
            "llk_czp.txt",
            "llk_dsc.dat",
            "llk_hint.lst",
            "llk_lcd",
            "llk_leftrite.txt",
            "llk_lln.dat",
            "llk_nam.dat",
            "llk_pno.dat",
            "llk_rv9.dat",
            "llk_rv9.esd",
            "llk_rv9.idx",
            "llk_sno.dat",
            "llk_strname.txt",
            "llk_suf.dat",
            "llk_urbx.lst",
            "llk_x11",
        };
        foreach (string file in lacsFiles)
        {
            File.Copy(Path.Combine(tempFolder, "LACS", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript\LACSLink\" + file, true);
        }

        List<string> suiteFiles = new()
        {
            "dvdhdr01.dat",
            "fileinfo_log.txt",
            "lcd",
            "slk.asc",
            "slk.ebc",
            "slkhdr01.dat",
            "slknine.lst",
            "slknoise.lst",
            "slknormal.lst",
            "slk_lcd",
        };
        foreach (string file in suiteFiles)
        {
            File.Copy(Path.Combine(tempFolder, "Suite", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript\SuiteLink\" + file, true);
        }

        List<string> zip4Files = new()
        {
            "ads_database.cfg",
            "cs_city.adb",
            "cs_city.als",
            "cs_city.voc",
            "cs_cs.adb",
            "cs_csb.adb",
            "cs_frgn.adb",
            "cs_frgn.voc",
            "cs_state.adb",
            "cs_state.als",
            "cs_state.voc",
            "cs_zip.voc",
            "live.txt",
            "st2zip.arr",
            "z4_apt.voc",
            "z4_ns.adb",
            "z4_pbns.voc",
            "z4_pobox.voc",
            "z4_rr.adb",
            "z4_rr.als",
            "z4_sdir.adb",
            "z4_sdir.als",
            "z4_sname.als",
            "z4_ssuf1.als",
            "z4_ssuf2.als",
            "z4_ssuff.adb",
            "zc2f.adb",
            "zfn.adb",
            "zip_info.adb",
            "zip_move.arr",
            "zip_patt.adb",
            "zip_tran.adb",
        };
        foreach (string file in zip4Files)
        {
            File.Copy(Path.Combine(tempFolder, "Zip4", file), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript\Zip4\" + file, true);
        }

        // Cleanup
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
    }

    private async Task RoyalMailInstallDirectory()
    {
        // Stop RAFMaster
        await Utils.StopService("RAFArgosyMaster");

        Utils.CopyFiles(discDrivePath, @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match-i");
    }
}
