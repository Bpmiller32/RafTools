using System.Diagnostics;
using System.Text;

namespace IsleBuilder.App;

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

    public static void KillProcs(object sender, EventArgs e)
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
}