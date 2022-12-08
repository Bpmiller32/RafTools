using System.Security.Principal;

namespace IoMDirectoryBuilder.Common;

public class Settings
{
    public string PafFilesPath { get; set; }
    public string SmiFilesPath { get; set; }
    public string DeployToAp { get; set; } = "FALSE";

    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }

    public void CheckArgs()
    {
        // Grab arguments from command line, format. Done for compatibility across different shells
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

        // Required argument checks
        if (string.IsNullOrEmpty(allArgs))
        {
            throw new ArgumentException("Missing required arguments");
        }
        if (arg1Start < 0 || arg1End < 0 || arg2Start < 0 || arg2End < 0)
        {
            throw new ArgumentException("Missing required argument PafFilesPath or SmiFilesPath");
        }
        if (arg1Start <= 1 || arg1End <= 1 || arg2Start <= 1 || arg2End <= 1)
        {
            throw new ArgumentException("Required arguments in incorrect order");
        }

        // Create and set separated and sanitized args
        string arg1 = allArgs[arg1Start..arg1End].Trim();
        string arg2 = allArgs[arg2Start..arg2End].Trim();
        string arg3 = allArgs[arg3Start..arg3End].Trim();

        PafFilesPath = arg1;
        SmiFilesPath = arg2;

        if (arg3 == "FALSE")
        {
            DeployToAp = arg3;
        }
        if (arg3 == "TRUE")
        {
            // Check for admin, error if admin isn't present
            WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
            bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isElevated)
            {
                throw new Exception("Application does not have administrator privledges, needed to deploy directory to Argosy Post");
            }

            DeployToAp = arg3;
        }

        // Optional argument check
        if (DeployToAp == "ERROR")
        {
            throw new ArgumentException("Invalid parameter for --DeployToAp");
        }
    }

    public void CheckPaths()
    {
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

        // Set working and output paths after passing previous checks
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
        List<string> pafMainFiles = new()
        {
            "bname.c01",
            "fpmainfl.c02",
            "fpmainfl.c03",
            "fpmainfl.c04",
            "fpmainfl.c05",
            "fpmainfl.c06",
            "local.c01",
            "mailsort.c01",
            "org.c01",
            "subbname.c01",
            "thdesc.c01",
            "thfare.c01",
            "wfmainfl.c06"
        };

        // Check to see if any files in the above lists are missing, if multiple missing grab all before throwing exception
        string missingFiles = "";

        foreach (string file in aliasFiles)
        {
            if (!File.Exists(Path.Combine(PafFilesPath, "ALIAS", file)))
            {
                missingFiles += file + ", ";
            }
        }
        foreach (string file in pafMainFiles)
        {
            if (!File.Exists(Path.Combine(PafFilesPath, "PAF MAIN FILE", file)))
            {
                missingFiles += file + ", ";
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
            "xerces-c_3_2.dll",
            "ConvertMainfileToCompStdConsole.exe",
            "EntityFramework.dll",
            "EntityFramework.SqlServer.dll",
            "log4net.dll",
            "MoreLinq.dll",
            "SqliteDbManager.dll",
            "System.Data.SQLite.dll",
            "System.Data.SQLite.EF6.dll",
            "System.Data.SQLite.Linq.dll",
            "System.ValueTuple.dll"
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