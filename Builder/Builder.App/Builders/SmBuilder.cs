namespace Builder.App.Builders;
using FlaUI.Core;
using FlaUI.UIA2;
using FlaUI.Core.AutomationElements;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Builder.App.Utils;

public class SmBuilder
{
    private readonly string inputPath;
    private readonly string outputPath;
    private readonly string month;
    private readonly string year;
    private readonly string user;
    private readonly string pass;
    private readonly DatabaseContext context;
    private readonly Action<int> progress;

    public SmBuilder(Settings settings, DatabaseContext context, Action<int> progress)
    {
        this.inputPath = settings.AddressDataPath;
        this.outputPath = settings.OutputPath;
        this.month = settings.DataMonth;
        this.year = settings.DataYear;
        this.user = settings.User;
        this.pass = settings.Pass;
        this.context = context;
        this.progress = progress;
    }

    public void Cleanup()
    {
        progress(2);
        Thread.Sleep(10000);
        progress(2);
        Thread.Sleep(10000);
        progress(2);
        Thread.Sleep(10000);
        

        Utils.KillSmProcs();

        // Ensure working and output directories are created and clear them if they already exist
        Directory.CreateDirectory(outputPath);

        DirectoryInfo op = new DirectoryInfo(outputPath);

        foreach (FileInfo file in op.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in op.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    public async Task Build()
    {
        Dictionary<string, Task> tasks = new Dictionary<string, Task>();

        tasks.Add("Build", Task.Run(() => BuildRunner()));
        tasks.Add("Timeout", Task.Run(async () => await Task.Delay(TimeSpan.FromMinutes(40))));

        if (await Task.WhenAny(tasks.Values) == tasks["Timeout"])
        {
            Utils.KillSmProcs();
            throw new Exception("Build process took longer than 30 minutes, error likely, check logs");
        }
        if (await Task.WhenAny(tasks.Values) == tasks["Build"])
        {
            if (tasks["Build"].Status == TaskStatus.Faulted)
            {
                throw new Exception("Build process ran into an error");
            }
        }
    }

    public void CheckBuildComplete()
    {
        UspsBundle bundle = context.UspsBundles.Where(x => (int.Parse(month) == x.DataMonth) && (int.Parse(year) == x.DataYear) && ("Cycle-N" == x.Cycle)).FirstOrDefault();
        bundle.IsBuildComplete = true;

        context.SaveChanges();
    }

    private async Task BuildRunner()
    {
        using (UIA2Automation automation = new UIA2Automation())
        {
            // Critical: Make sure you are in the XtlBuilder directory...
            Directory.SetCurrentDirectory(@"C:\ProgramData\RAF\XtlBuildingWizard");

            // Launch app
            Application app = Application.Launch(@"C:\ProgramData\RAF\XtlBuildingWizard\XtlBuildingWizard.exe");

            // Wait a few seconds for "splash screen" effect
            await Task.Delay(TimeSpan.FromSeconds(3));
            Window window = app.GetMainWindow(automation);

            // Check that main window elements can be found
            AutomationElement nextButton = window.FindFirstDescendant(cf => cf.ByName(@"Next"));
            if (nextButton == null)
            {
                throw new Exception("Could not find the window elements");
            }

            int id = app.ProcessId;

            // 1st page
            nextButton.AsButton().Invoke();
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 2nd page
            app = Application.Attach(id);
            Window[] newWindows = app.GetAllTopLevelWindows(automation);
            var editBoxes = newWindows[0].FindAllDescendants(cf => cf.ByLocalizedControlType(@"edit"));

            // Have to edit in this order or the rest autofill with values....
            editBoxes[5].AsTextBox().Enter(year.Substring(2, 2) + month + @"1");
            editBoxes[2].AsTextBox().Enter(inputPath);
            editBoxes[4].AsTextBox().Enter(user);
            editBoxes[3].AsTextBox().Enter(pass);
            editBoxes[0].AsTextBox().Enter(Path.Combine(outputPath, year + month + @"_SHA2"));

            AutomationElement buildButton = newWindows[0].FindFirstDescendant(cf => cf.ByName(@"Build"));
            buildButton.AsButton().Invoke();

            await WaitForBuild(newWindows[0]);

            newWindows[0].Close();

            progress(2);
        }

    }

    private async Task WaitForBuild(Window window)
    {
        Regex stage = new Regex(@"(Stage \d)");
        Regex package = new Regex(@"(Packaged )(\w+)( data)");
        Regex finish = new Regex(@"(Successfully built XTLs to)");

        AutomationElement statusBox = window.FindFirstDescendant(cf => cf.ByName(@"XTL test data:"));
        AutomationElement[] logs = statusBox.FindAllDescendants();

        foreach (var log in logs)
        {
            Match matchStage = stage.Match(log.Name);
            Match matchPackaging = package.Match(log.Name);
            Match matchfinish = finish.Match(log.Name);

            if (matchStage.Success == true)
            {
                progress(10);
            }

            if (matchPackaging.Success == true)
            {
                progress(9);
            }

            if (matchfinish.Success == true)
            {
                progress(2);
                return;
            }
        }

        await Task.Delay(TimeSpan.FromMinutes(2));
        await WaitForBuild(window);
    }
}
