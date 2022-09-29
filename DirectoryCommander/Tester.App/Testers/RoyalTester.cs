using System.Diagnostics;
using System.Text.RegularExpressions;
using Common.Data;

namespace Tester
{
    public class RoyalTester
    {
        public Settings Settings { get; set; } = new Settings { Name = "RoyalMail" };
        public ComponentStatus Status { get; set; }
        public int Progress { get; set; }

        private readonly ILogger<RoyalTester> logger;
        private readonly IConfiguration config;

        public RoyalTester(ILogger<RoyalTester> logger, IConfiguration config)
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

            if (!File.Exists(Path.Combine(Settings.DiscDrivePath, rmSettingsFile)))
            {
                missingFiles += rmSettingsFile + ", ";
            }
            foreach (string file in rmFiles)
            {
                if (!File.Exists(Path.Combine(Settings.DiscDrivePath, "UK_RM_CM", file)))
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

            Utils.CopyFiles(Settings.DiscDrivePath, @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match-i");
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

                if (line.Contains("SM-i Adaptor     Sub-product \"UK_RM_CM - 3.0\" is not licensed: The system dongle was not found in the LCS file"))
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
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.txt")))
            {
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.txt"));
            }
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.lcs")))
            {
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.lcs"));
            }

            // Guess set dataYearMonth to current year and month (no way to find from directory without initiallizing, can't initialize without dongle on lcs....)
            string year = DateTime.Now.Year.ToString();
            int monthInt = DateTime.Now.Month;

            string month = "";
            if (monthInt < 10)
            {
                month += "0" + monthInt.ToString();
            }
            else
            {
                month += monthInt.ToString();
            }

            string dataYearMonth = string.Concat(year, month, "19");

            // Create txt version of ArgosyMonthly that includes dongle
            using (StreamWriter sw = new(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.txt"), true))
            {
                sw.WriteLine("Date=" + dataYearMonth);
                sw.WriteLine("Directory=UK_RM_CM");
                sw.WriteLine("Dongles:");
                sw.WriteLine(Settings.DongleId);
            }

            // Encrypt new Uk dongle list, but first wrap the combined paths in quotes to get around spaced directories
            string encryptRepFileName = Path.Combine(Directory.GetCurrentDirectory(), "EncryptREP.exe");
            const string encryptRepArgs = "-x lcs UK_RM_CM.txt";

            // Remember to add EncryptREP to Windows Defender exclusion list
            Process encryptRep = Utils.RunProc(encryptRepFileName, encryptRepArgs);
            encryptRep.WaitForExit();

            // Check that LCS file was actually created
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.lcs")))
            {
                throw new Exception("LCS file was not created, something likely wrong with EncryptREP");
            }

            // Overwrite old LCS, start RAFArgosyMaster
            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.lcs"), @"C:\ProgramData\RAF\ArgosyPost\Sync\Directories\RAF Smart Match-i\UK_RM_CM\UK_RM_CM.lcs", true);

            // Cleanup
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.txt")))
            {
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.txt"));
            }
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.lcs")))
            {
                File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "UK_RM_CM.lcs"));
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
}