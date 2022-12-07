using System.Diagnostics;
using System.Text;

namespace IoMDirectoryBuilder.Common;

public static class Utils
{
    public static string WrapQuotes(string input)
    {
        StringBuilder sb = new();
        sb.Append('"').Append(input).Append('"');
        return sb.ToString();
    }

    public static void CopyFiles(string sourceDirectory, string destDirectory, CancellationToken stoppingToken)
    {
        DirectoryInfo source = new(sourceDirectory);
        DirectoryInfo dest = new(destDirectory);

        // Need the helper because of recursive call
        CopyFilesHelper(source, dest, stoppingToken);
    }

    public static void CopyFilesHelper(DirectoryInfo source, DirectoryInfo dest, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(dest.FullName);

        foreach (FileInfo file in source.GetFiles())
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                file.CopyTo(Path.Combine(dest.FullName, file.Name), true);
            }
        }

        foreach (DirectoryInfo subDir in source.GetDirectories())
        {
            DirectoryInfo nextSubDir = dest.CreateSubdirectory(subDir.Name);
            CopyFilesHelper(subDir, nextSubDir, stoppingToken);
        }
    }

    public static void MoveFiles(string sourceDirectory, string destDirectory, CancellationToken stoppingToken)
    {
        DirectoryInfo source = new(sourceDirectory);
        DirectoryInfo dest = new(destDirectory);

        // Need the helper because of recursive call
        MoveFilesHelper(source, dest, stoppingToken);
    }

    public static void MoveFilesHelper(DirectoryInfo source, DirectoryInfo dest, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(dest.FullName);

        foreach (FileInfo file in source.GetFiles())
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                file.MoveTo(Path.Combine(dest.FullName, file.Name), true);
            }
        }

        foreach (DirectoryInfo subDir in source.GetDirectories())
        {
            DirectoryInfo nextSubDir = dest.CreateSubdirectory(subDir.Name);
            MoveFilesHelper(subDir, nextSubDir, stoppingToken);
        }
    }

    public static Process RunProc(string fileName, string args)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process proc = new()
        {
            StartInfo = startInfo
        };

        proc.Start();

        return proc;
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
        foreach (Process process in Process.GetProcessesByName("ConvertMainfileToCompStdConsole"))
        {
            process.Kill(true);
        }
    }

    // Overload for ProcessExit event handler
    public static void KillRmProcs(object sender, EventArgs e)
    {
        KillRmProcs();
    }
}