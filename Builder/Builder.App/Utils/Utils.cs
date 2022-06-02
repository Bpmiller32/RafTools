using System.Diagnostics;
using System.Text;

public class Utils
{
    public static string WrapQuotes(string input)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("\"").Append(input).Append("\"");
        return sb.ToString();
    }

    public static void CopyFiles(string sourceDirectory, string destDirectory)
    {
        DirectoryInfo source = new DirectoryInfo(sourceDirectory);
        DirectoryInfo dest = new DirectoryInfo(destDirectory);

        CopyFilesHelper(source, dest);
    }

    public static void CopyFilesHelper(DirectoryInfo source, DirectoryInfo dest)
    {
        Directory.CreateDirectory(dest.FullName);

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(dest.FullName, file.Name), true);
        }

        foreach (DirectoryInfo subDir in source.GetDirectories())
        {
            DirectoryInfo nextSubDir = dest.CreateSubdirectory(subDir.Name);
            CopyFilesHelper(subDir, nextSubDir);
        }
    }

    public static Process RunProc(string fileName, string args)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process proc = new Process()
        {
            StartInfo = startInfo
        };

        proc.Start();

        return proc;
    }

    public static void KillSmProcs()
    {
        foreach (Process process in Process.GetProcessesByName("CleanupDatabase"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("DBCreate"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("ImportUsps"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("GenerateUspsXtls"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("GenerateKeyXtl"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("DumpKeyXtl"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("DumpXtlHeader"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("EncryptREP"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("TestXtlsN2"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("AddDpvHeader"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("rafatizeSLK"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("XtlBuildingWizard"))
        {
            process.Kill(true);
        }
    }

    public static void KillPsProcs()
    {
        foreach (var process in Process.GetProcessesByName("PDBIntegrity"))
        {
            process.Kill(true);
        }
    }

    public static void KillRmProcs()
    {
        foreach (Process process in Process.GetProcessesByName("ConvertPafData"))
        {
            process.Kill(true);
        }
        foreach (Process process in Process.GetProcessesByName("DirectoryDataCompiler"))
        {
            process.Kill(true);
        }
    }

    public static void KillAllProcs(object sender, EventArgs e)
    {
        KillSmProcs();
        KillPsProcs();
        KillRmProcs();
    }
}
