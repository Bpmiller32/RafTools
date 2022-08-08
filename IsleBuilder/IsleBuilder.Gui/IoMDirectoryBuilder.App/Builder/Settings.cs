namespace IoMDirectoryBuilder.App;

public class Settings
{
    public string PafFilesPath { get; set; }
    public string SmiFilesPath { get; set; }

    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }

    public void CheckPaths()
    {
        // Check if input is empty
        if (string.IsNullOrEmpty(PafFilesPath))
        {
            throw new Exception("Path to PAF files not valid");
        }
        if (string.IsNullOrEmpty(SmiFilesPath))
        {
            throw new Exception("Path to SMi files not valid");
        }
        // Check if input in wrapped in path quotes
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
            throw new Exception("Path to PAF files does not exist");
        }
        if (!Directory.Exists(SmiFilesPath))
        {
            throw new Exception("Path to SMi files does not exist");
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
            if (!File.Exists(Path.Combine(PafFilesPath, "ALIAS", file)))
            {
                missingFiles += file + ", ";
            }
        }
        foreach (string file in csvBfpoFiles)
        {
            if (!File.Exists(Path.Combine(PafFilesPath, "CSV BFPO", file)))
            {
                missingFiles += file + ", ";
            }
        }
        foreach (string file in pafCompressedStdFiles)
        {
            if (!File.Exists(Path.Combine(PafFilesPath, "PAF COMPRESSED STD", file)))
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
