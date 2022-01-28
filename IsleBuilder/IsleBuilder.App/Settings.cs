using System.ServiceProcess;

#pragma warning disable CA1416 // ignore that my calls to manipulate services and check for admin are Windows only 

namespace IsleBuilder.App;

public class Settings
{
    public const string IoM = nameof(IoM);

    public string AddressDataPath { get; set; }
    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }

    public string BuildToolsPath { get; set; }
    public string BuildFilesPath { get; set; }

    public bool MoveToAp { get; set; }

    public static Settings Validate(Settings settings)
    {
        // Check that appsettings.json exists at all
        if (!File.Exists(Directory.GetCurrentDirectory() + @"\appsettings.json"))
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



        // If WorkingPath is empty in appsettings set to default
        if (String.IsNullOrEmpty(settings.WorkingPath))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\IoM-Working");
            settings.WorkingPath = Directory.GetCurrentDirectory() + @"\IoM-Working";
        }
        // If OutputPath is empty in appsettings set to default
        if (String.IsNullOrEmpty(settings.OutputPath))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\IoM-Output");
            settings.OutputPath = Directory.GetCurrentDirectory() + @"\IoM-Output";
        }
        // If ToolsPath is empty in appsettings set to default
        if (String.IsNullOrEmpty(settings.BuildToolsPath))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\SmiBuildTools");
            settings.BuildToolsPath = Directory.GetCurrentDirectory() + @"\SmiBuildTools";
        }
        // If SmiBuildFilesPath is empty in appsettings set to default
        if (String.IsNullOrEmpty(settings.BuildFilesPath))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\SmiBuildFiles");
            settings.BuildFilesPath = Directory.GetCurrentDirectory() + @"\SmiBuildFiles";
        }
        

        
        // Check for missing files
        CheckMissingPafFiles(settings);
        CheckMissingSmiFiles(settings);
        CheckMissingToolFiles(settings);
        // CheckForAp();

        return settings;
    }

    private static void CheckMissingPafFiles(Settings settings)
    {
        // List of files needed to compile, listing them out this way in case this changes in the future
        List<string> aliasFiles = new List<string>
        {
            @"aliasfle.c01"
        };
        List<string> bfpoFiles = new List<string>
        {
            @"CSV BFPO.csv"
        };
        List<string> stdFiles = new List<string>
        {
            @"wfcompst.c15",
            @"fpcompst.c01",
            @"fpcompst.c02",
            @"fpcompst.c03",
            @"fpcompst.c04",
            @"fpcompst.c05",
            @"fpcompst.c06",
            @"fpcompst.c07",
            @"fpcompst.c08",
            @"fpcompst.c09",
            @"fpcompst.c10",
            @"fpcompst.c11",
            @"fpcompst.c12",
            @"fpcompst.c13",
            @"fpcompst.c14",
            @"fpcompst.c15",
        };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (var file in aliasFiles)
        {
            if (!File.Exists(settings.AddressDataPath + @"\ALIAS\" + file))
            {
                missingFiles += file + ", ";
            }
        }
        foreach (var file in bfpoFiles)
        {
            if (!File.Exists(settings.AddressDataPath + @"\CSV BFPO\" + file))
            {
                missingFiles += file + ", ";
            }
        }
        foreach (var file in stdFiles)
        {
            if (!File.Exists(settings.AddressDataPath + @"\PAF COMPRESSED STD\" + file))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing address data files needed for compile: " + missingFiles + @". Check AddressData folder");
        }
    }

    private static void CheckMissingSmiFiles(Settings settings)
    {
        List<string> smiFiles = new List<string> 
        { 
            @"UK_RM_CM.xml", 
            @"UK_RM_CM_Patterns.xml", 
            @"UK_RM_CM_Settings.xml", 
            @"BFPO.txt", 
            @"Country.txt", 
            @"County.txt", 
            @"PostTown.txt", 
            @"StreetDescriptor.txt", 
            @"StreetName.txt", 
            @"PoBoxName.txt", 
            @"SubBuildingDesignator.txt", 
            @"OrganizationName.txt", 
            @"Country_Alias.txt", 
            @"UK_IgnorableWordsTable.txt", 
            @"UK_WordMatchTable.txt"
        };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (var file in smiFiles)
        {
            if (!File.Exists(settings.BuildFilesPath + @"\" + file))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing SMi build files needed for compile: " + missingFiles + @". Check SmiBuildFiles folder");
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
            if (!File.Exists(settings.BuildToolsPath + @"\3.0\" + file))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new Exception(@"Missing SMi Tool files needed for compile: " + missingFiles + @". Check BuildTools folder");
        }
    }

    private static void CheckForAp()
    {
        ServiceController[] services = ServiceController.GetServices(Environment.MachineName);
        ServiceController rafMasterService = services.FirstOrDefault(s => s.ServiceName == "RAFArgosyMaster");

        if (rafMasterService == null)
        {
            throw new Exception("Argosy Post Master service not detected on this system. Please check the Argosy Post installation");
        }
    }
}
