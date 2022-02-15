namespace Builder.App.Utils;

public class Settings
{
    public const string SmartMatch = nameof(SmartMatch);
    public const string Parascript = nameof(Parascript);
    public const string RoyalMail = nameof(RoyalMail);

    public string Name { get; set; }
    public string AddressDataPath { get; set; }
    // Can optionally set WorkingPath and OutputPath in appsettings
    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }
    // Will be overridden every time by BE SocketMessage
    public string User { get; set; }
    public string Pass { get; set; }
    public string Key { get; set; }
    public string DataMonth { get; set; }
    public string DataYear { get; set; }

    public static Settings Validate(Settings settings, SocketMessage message)
    {
        // Check that appsettings.json exists at all
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), @"appsettings.json")))
        {
            throw new Exception("appsettings.json is missing, make sure there is a valid appsettings.json file in the same directory as the application");
        }
        // Check that a directory with PAF data is provided
        if (String.IsNullOrEmpty(settings.AddressDataPath))
        {
            throw new Exception("No path to address data provided, check appsettings.json");
        }
        // Check if provided directory exists on the filesystem
        if (!Directory.Exists(settings.AddressDataPath))
        {
            throw new Exception("Address data directory provided doesn't exist: " + settings.AddressDataPath);
        }
        // Check that there are files in the provided directory
        if (!Directory.EnumerateFileSystemEntries(settings.AddressDataPath).Any())
        {
            throw new Exception("Address data directory provided is empty: " + settings.AddressDataPath);
        }



        // Set input path from base directory path
        if (int.Parse(message.Month) < 10)
        {
            message.Month = "0" + message.Month;
        }
        settings.DataMonth = message.Month;
        settings.DataYear = message.Year;
        string dataYearMonth = message.Year + message.Month;
        
        settings.AddressDataPath = Path.Combine(settings.AddressDataPath, dataYearMonth);
        // If WorkingPath is empty in appsettings set to default
        if (String.IsNullOrEmpty(settings.WorkingPath))
        {
            if (settings.Name != "SM")
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), settings.Name + @"-Working"));
                settings.WorkingPath = Path.Combine(Directory.GetCurrentDirectory(), settings.Name + @"-Working");                
            }
        }
        // If OutputPath is empty in appsettings set to default
        if (String.IsNullOrEmpty(settings.OutputPath))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), settings.Name, @"-Output", dataYearMonth));
            settings.OutputPath = Directory.GetCurrentDirectory() + @"\" + settings.Name + @"-Output";
        }



        // Directory specific
        settings.User = message.SmUser;
        settings.Pass = message.SmPass;
        settings.Key = message.Key;

        if (settings.Name == "SM" && (String.IsNullOrEmpty(settings.User) || String.IsNullOrEmpty(settings.Pass)))
        {   
            throw new Exception("Missing a Username/Password/Key for: " + settings.Name);            
        }
        if (settings.Name == "RM" && String.IsNullOrEmpty(settings.Key))
        {
            throw new Exception("Missing a Username/Password/Key for: " + settings.Name);            
        }



        // Check for any missing files
        if (settings.Name == "SM")
        {
            CheckMissingSmFiles(settings);
        }
        if (settings.Name == "PS")
        {
            CheckMissingPsFiles(settings);
        }
        if (settings.Name == "RM")
        {
            CheckMissingRmFiles(settings);
            CheckMissingToolFiles(settings);
        }

        return settings;
    }

    public static void CheckMissingSmFiles(Settings settings)
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
            if (!File.Exists(Path.Combine(settings.AddressDataPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing address data files needed for compile: " + missingFiles);
        }
    }

    public static void CheckMissingPsFiles(Settings settings)
    {
        // TODO: Regex match this
    }

    public static void CheckMissingRmFiles(Settings settings)
    {
        List<string> rmFiles = new List<string>
        {
            @"SetupRM.exe"
        };

        string missingFiles = "";

        foreach (string file in rmFiles)
        {
            if (!File.Exists(Path.Combine(settings.AddressDataPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing address data files needed for compile: " + missingFiles);
        }
    }

    private static void CheckMissingToolFiles(Settings settings)
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
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), @"BuildUtils", "3.0" , file)))
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
