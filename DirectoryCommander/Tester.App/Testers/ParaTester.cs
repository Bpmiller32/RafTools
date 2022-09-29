using System.Diagnostics;
using System.IO.Compression;
using Common.Data;

namespace Tester
{
    public class ParaTester
    {
        public Settings Settings { get; set; } = new Settings { Name = "Parascript" };
        public ComponentStatus Status { get; set; }
        public int Progress { get; set; }

        private readonly ILogger<ParaTester> logger;
        private readonly IConfiguration config;

        public ParaTester(ILogger<ParaTester> logger, IConfiguration config)
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
                "llkhdr02.dat",
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

            if (!File.Exists(Path.Combine(Settings.DiscDrivePath, "DPV", dpvFile)))
            {
                missingFiles += dpvFile + ", ";
            }
            if (!File.Exists(Path.Combine(Settings.DiscDrivePath, "Suite", suiteFile)))
            {
                missingFiles += suiteFile + ", ";
            }
            if (!File.Exists(Path.Combine(Settings.DiscDrivePath, "Zip4", zip4File)))
            {
                missingFiles += zip4File + ", ";
            }
            foreach (string file in lacsFiles)
            {
                if (!File.Exists(Path.Combine(Settings.DiscDrivePath, "LACS", file)))
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
            Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "DPV"));
            Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "LACSLink"));
            Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "SuiteLink"));
            Directory.CreateDirectory(Path.Combine(@"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\AddressScript", "Zip4"));

            ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "Zip4", "Zip4.zip"), Path.Combine(tempFolder, "Zip4"));
            ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "Suite", "SUITE.zip"), Path.Combine(tempFolder, "Suite"));
            ZipFile.ExtractToDirectory(Path.Combine(Settings.DiscDrivePath, "DPV", "DPV.zip"), Path.Combine(tempFolder, "DPV"));
            Utils.CopyFiles(Path.Combine(Settings.DiscDrivePath, "LACS"), Path.Combine(tempFolder, "LACS"));

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
                // Wait for AP initialize
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
}