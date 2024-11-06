using System.Security.Principal;

namespace IoMDirectoryBuilder.Application;

static class Program
{
    [STAThread]
    static void Main()
    {
        const string applicationName = "IoMDirectoryBuilder";
        using var mutex = new Mutex(false, applicationName);

        // Single instance of application check
        bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);

        // Check for admin
        WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
        bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainWindow(isAnotherInstanceOpen, isElevated));
    }
}