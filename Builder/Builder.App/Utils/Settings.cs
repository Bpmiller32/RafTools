public class Settings
{
    public string Name { get; set; }
    public string AddressDataPath { get; set; }
    // Can optionally set WorkingPath and OutputPath in appsettings
    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }
    // ----------
    public bool BuilderEnabled { get; set; }
    public bool AutoBuildEnabled { get; set; }

    public string UserName { get; set; }
    public string Password { get; set; }
    public string Key { get; set; }
    public string DataYearMonth { get; set; }

    public int ExecYear { get; set; }
    public int ExecMonth { get; set; }
    public int ExecDay { get; set; }
    public int ExecHour { get; set; }
    public int ExecMinute { get; set; }
    public int ExecSecond { get; set; }

    public static TimeSpan CalculateWaitTime(ILogger logger, Settings settings)
    {
        DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
        DateTime tomorrow = today.AddDays(1);

        TimeSpan waitToday = today - DateTime.Now;
        TimeSpan waitTomorrow = tomorrow - DateTime.Now;

        if (waitToday.TotalSeconds <= 0)
        {
            logger.LogInformation("Waiting for pass, starting sleep until : " + today);
            return waitToday;
        }

        logger.LogInformation("Waiting for pass, starting sleep until: " + tomorrow);
        return waitTomorrow;
    }

    public void Validate(IConfiguration config, string DataYearMonth)
    {
        // Check that appsettings.json exists at all
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), @"appsettings.json")))
        {
            throw new Exception("appsettings.json is missing, make sure there is a valid appsettings.json file in the same directory as the application");
        }
        // Check that BuildUtils exist
        if (!Directory.EnumerateFileSystemEntries(Directory.GetCurrentDirectory() + @"\BuildUtils").Any())
        {
            throw new Exception("BuildUtils folder is missing");
        }
        // Check that DataYearMonth is provided
        if (string.IsNullOrEmpty(DataYearMonth))
        {
            throw new Exception("DataYearMonth not provided");
        }
        else
        {
            this.DataYearMonth = DataYearMonth;
        }

        // Verify for each directory
        List<string> directories = new List<string>() { "SmartMatch", "Parascript", "RoyalMail" };
        foreach (string dir in directories)
        {
            if (dir == Name)
            {
                // If WorkingPath is empty in appsettings set to default
                if (string.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":WorkingPath")))
                {
                    if (Name != "SmartMatch")
                    {
                        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), @"Working", Name));
                        WorkingPath = Path.Combine(Directory.GetCurrentDirectory(), @"Working", Name);
                    }
                }
                // If OutputPath is empty in appsettings set to default
                if (string.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":OutputPath")))
                {
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), @"Output", Name, DataYearMonth));
                    OutputPath = Path.Combine(Directory.GetCurrentDirectory(), @"Output", Name, DataYearMonth);
                }


                // AddressDataPath checks
                if (string.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":AddressDataPath")))
                {
                    throw new Exception("No path to address data provided, check appsettings.json");
                }
                else
                {
                    AddressDataPath = Path.Combine(config.GetValue<string>("settings:" + dir + ":AddressDataPath"), DataYearMonth);
                }
                // Check if provided directory exists on the filesystem
                if (!Directory.Exists(AddressDataPath))
                {
                    throw new Exception("Address data directory provided doesn't exist: " + AddressDataPath);
                }
                // Check that there are files in the provided directory
                if (!Directory.EnumerateFileSystemEntries(AddressDataPath).Any())
                {
                    throw new Exception("Address data directory provided is empty: " + AddressDataPath);
                }
                // SmartMatch specific, look into cycle
                if (Name == "SmartMatch")
                {
                    AddressDataPath = Path.Combine(AddressDataPath, @"Cycle-N");
                }

                // Enabled checks
                if (config.GetValue<bool>("settings:" + dir + ":BuilderEnabled"))
                {
                    BuilderEnabled = true;
                }
                if (config.GetValue<bool>("settings:" + dir + ":AutoBuildEnabled"))
                {
                    AutoBuildEnabled = true;
                }

                // Autobuild time and date checks
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Year") != 0)
                {
                    ExecYear = config.GetValue<int>("settings:" + dir + ":ExecTime:Year");
                }
                else
                {
                    ExecYear = DateTime.Now.Year;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Month") != 0)
                {
                    ExecMonth = config.GetValue<int>("settings:" + dir + ":ExecTime:Month");
                }
                else
                {
                    ExecMonth = DateTime.Now.Month;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Day") != 0)
                {
                    ExecDay = config.GetValue<int>("settings:" + dir + ":ExecTime:Day");
                }
                else
                {
                    ExecDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Hour") != 0)
                {
                    ExecHour = config.GetValue<int>("settings:" + dir + ":ExecTime:Hour");
                }
                else
                {
                    ExecHour = 11;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Minute") != 0)
                {
                    ExecMinute = config.GetValue<int>("settings:" + dir + ":ExecTime:Minute");
                }
                else
                {
                    ExecMinute = 59;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Second") != 0)
                {
                    ExecSecond = config.GetValue<int>("settings:" + dir + ":ExecTime:Second");
                }
                else
                {
                    ExecSecond = 59;
                }

                // Check that day hasn't passed, display next month
                if (ExecDay < DateTime.Now.Day)
                {
                    ExecMonth = DateTime.Now.AddMonths(1).Month;
                }
            }
        }

        // Check for any missing files
        if (Name == "SmartMatch")
        {
            CheckMissingSmFiles();
        }
        if (Name == "Parascript")
        {
            CheckMissingPsFiles();
        }
        if (Name == "RoyalMail")
        {
            CheckMissingRmFiles();
            CheckMissingToolFiles();
        }
    }

    public void CheckMissingSmFiles()
    {
        List<string> smFiles = new List<string>
        {
            @"dpvfl2.tar",
            @"dpvsp2.tar",
            @"laclnk2.tar",
            @"stelnk2.tar",
            @"zip4natl.tar",
            @"zipmovenatl.tar"
        };

        string missingFiles = "";

        foreach (string file in smFiles)
        {
            if (!File.Exists(Path.Combine(AddressDataPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing address data files needed for compile: " + missingFiles);
        }
    }

    public void CheckMissingPsFiles()
    {
        List<string> psFiles = new List<string>
        {
            @"Files.zip"
        };

        string missingFiles = "";

        foreach (string file in psFiles)
        {
            if (!File.Exists(Path.Combine(AddressDataPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing address data files needed for compile: " + missingFiles);
        }
    }

    public void CheckMissingRmFiles()
    {
        List<string> rmFiles = new List<string>
        {
            @"SetupRM.exe"
        };

        string missingFiles = "";

        foreach (string file in rmFiles)
        {
            if (!File.Exists(Path.Combine(AddressDataPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing address data files needed for compile: " + missingFiles);
        }
    }

    private void CheckMissingToolFiles()
    {
        List<string> toolFiles = new List<string>
        {
            @"BrazilPostProcessor.dll",
            @"dafs.dll",
            @"DirectoryDataCompiler.exe",
            @"SMI.dll",
            @"Smi.xsd",
            @"UkPostProcessor.dll",
            @"xerces-c_3_1.dll"
        };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (var file in toolFiles)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), @"BuildUtils", "3.0", file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing SMi Tool files needed for compile: " + missingFiles);
        }
    }
}
