namespace IoMDirectoryBuilder.Common;

public class Settings
{
    public string PafFilesPath { get; set; }
    public string SmiFilesPath { get; set; }
    public string DeployToAp { get; set; } = "FALSE";

    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }

    public Dictionary<string, string> PafLocations { get; set; } = new();

    public void CheckArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        string allArgs = "";
        for (int i = 1; i < args.Length; i++)
        {
            allArgs += " " + args[i];
        }

        // Convert toUpper to eliminate case differences, remove quotes
        allArgs = allArgs.ToUpper();
        allArgs = allArgs.Replace("\"", string.Empty);

        // Find beginning and ends of args
        int arg1Start = allArgs.IndexOf("--PAFFILESPATH") + 14;
        int arg2Start = allArgs.IndexOf("--SMIFILESPATH") + 14;
        int arg3Start = allArgs.IndexOf("--DEPLOYTOAP") + 12;

        int arg1End = allArgs.IndexOf("--SMIFILESPATH");
        int arg2End = allArgs.Length;
        int arg3End = 11;

        // Find if arg3 exists, set length of arg2 and arg3 depending
        bool arg3Exists = arg3Start != 11;
        if (arg3Exists)
        {
            arg2End = allArgs.IndexOf("--DEPLOYTOAP");
            arg3End = allArgs.Length;
            DeployToAp = "ERROR";
        }

        // Create and set separated and sanitized args
        string arg1 = allArgs[arg1Start..arg1End].Trim();
        string arg2 = allArgs[arg2Start..arg2End].Trim();
        string arg3 = allArgs[arg3Start..arg3End].Trim();

        PafFilesPath = arg1;
        SmiFilesPath = arg2;

        if (arg3 == "TRUE" || arg3 == "FALSE")
        {
            DeployToAp = arg3;
        }
    }

    public void CheckPaths()
    {
        // Check if both inputs are empty, if so show Usage()
        if (string.IsNullOrEmpty(PafFilesPath) && string.IsNullOrEmpty(SmiFilesPath))
        {
            throw new ArgumentNullException();
        }
        // Check if individual input is empty
        if (string.IsNullOrEmpty(PafFilesPath))
        {
            throw new ArgumentException("Invalid parameter for --PafFilesPath");
        }
        if (string.IsNullOrEmpty(SmiFilesPath))
        {
            throw new ArgumentException("Invalid parameter for --SmiFilesPath");
        }
        if (DeployToAp == "ERROR")
        {
            throw new ArgumentException("Invalid parameter for --DeployToAp");
        }
        // Check if input in wrapped in path quotes (for WinForms verison)
        if (PafFilesPath.StartsWith('"') && PafFilesPath.EndsWith('"'))
        {
            PafFilesPath = PafFilesPath.Replace("\"", string.Empty);
        }
        if (SmiFilesPath.StartsWith('"') && SmiFilesPath.EndsWith('"'))
        {
            SmiFilesPath = SmiFilesPath.Replace("\"", string.Empty);
        }
        // Check that path exists on disk
        if (!Directory.Exists(PafFilesPath))
        {
            throw new ArgumentException("Invalid parameter for --PafFilesPath");
        }
        if (!Directory.Exists(SmiFilesPath))
        {
            throw new ArgumentException("Invalid parameter for --SmiFilesPath");
        }

        // Create working and output paths after passing previous checks
        WorkingPath = Path.Combine(SmiFilesPath, "Temp");
        OutputPath = Path.Combine(SmiFilesPath, "Output");

        // Set exe directory to SmiFilesPath, makes ConvertPafData and DirectoryDataCompiler logs land in SMi folder
        Directory.SetCurrentDirectory(SmiFilesPath);
    }

    public void CheckMissingPafFiles()
    {
        // Files to check for by extracted folder
        List<string> aliasFiles = new()
    {
        "aliasfle.c01"
    };
        List<string> csvBfpoFiles = new()
    {
        "CSV BFPO.csv"
    };
        List<string> pafCompressedStdFiles = new()
    {
        "fpcompst.c01",
        "fpcompst.c02",
        "fpcompst.c03",
        "fpcompst.c04",
        "fpcompst.c05",
        "fpcompst.c06",
        "fpcompst.c07",
        "fpcompst.c08",
        "fpcompst.c09",
        "fpcompst.c10",
        "fpcompst.c11",
        "fpcompst.c12",
        "fpcompst.c13",
        "fpcompst.c14",
        "fpcompst.c15",
        "wfcompst.c15"
    };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (string file in aliasFiles)
        {
            if (File.Exists(Path.Combine(PafFilesPath, "ALIAS", file)))
            {
                PafLocations.Add(file, Path.Combine(PafFilesPath, "ALIAS", file));
            }
            else
            {
                if (File.Exists(Path.Combine(PafFilesPath, file)))
                {
                    PafLocations.Add(file, Path.Combine(PafFilesPath, file));
                }
                else
                {
                    missingFiles += file + ", ";
                }
            }
        }
        foreach (string file in csvBfpoFiles)
        {
            if (File.Exists(Path.Combine(PafFilesPath, "CSV BFPO", file)))
            {
                PafLocations.Add(file, Path.Combine(PafFilesPath, "CSV BFPO", file));
            }
            else
            {
                if (File.Exists(Path.Combine(PafFilesPath, file)))
                {
                    PafLocations.Add(file, Path.Combine(PafFilesPath, file));
                }
                else
                {
                    missingFiles += file + ", ";
                }
            }
        }
        foreach (string file in pafCompressedStdFiles)
        {
            if (File.Exists(Path.Combine(PafFilesPath, "PAF COMPRESSED STD", file)))
            {
                PafLocations.Add(file, Path.Combine(PafFilesPath, "PAF COMPRESSED STD", file));
            }
            else
            {
                if (File.Exists(Path.Combine(PafFilesPath, file)))
                {
                    PafLocations.Add(file, Path.Combine(PafFilesPath, file));
                }
                else
                {
                    missingFiles += file + ", ";
                }
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new FileNotFoundException("Missing PAF data files needed for compile: " + missingFiles);
        }
    }

    public void CheckMissingSmiFiles()
    {
        // Files to check for
        List<string> smiFiles = new()
    {
        "BFPO.txt",
        "Country.txt",
        "Country_Alias.txt",
        "County.txt",
        "IsleOfMan.xml",
        "IsleOfMan_CharMatchTable.txt",
        "IsleOfMan_IgnorableWordsTable.txt",
        "IsleOfMan_Patterns.exml",
        "IsleOfMan_Settings.xml",
        "IsleOfMan_WordMatchTable.txt",
        "OrganizationName.txt",
        "PoBoxName.txt",
        "PostTown.txt",
        "StreetDescriptor.txt",
        "StreetName.txt",
        "SubBuildingDesignator.txt"
    };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (string file in smiFiles)
        {
            if (!File.Exists(Path.Combine(SmiFilesPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new FileNotFoundException("Missing SMi data files needed for compile: " + missingFiles);
        }
    }

    public void CheckMissingToolFiles()
    {
        // Files to check for
        List<string> toolFiles = new()
    {
        "Dafs.dll",
        "DirectoryDataCompiler.exe",
        "ConvertPafData.exe",
        "SMI.dll",
        "Smi.xsd",
        "UkPostProcessor.dll",
        "xerces-c_3_2.dll"
    };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (string file in toolFiles)
        {
            if (!File.Exists(Path.Combine(SmiFilesPath, file)))
            {
                missingFiles += file + ", ";
            }
        }

        if (!string.IsNullOrEmpty(missingFiles))
        {
            throw new FileNotFoundException("Missing SMi tool files needed for compile: " + missingFiles);
        }
    }
}