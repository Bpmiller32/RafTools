namespace IoMDirectoryBuilder.App;

public partial class MainWindow : Form
{
    private readonly Settings settings;
    private readonly PafBuilder builder;
    private readonly bool isElevated;

    public MainWindow(bool isAnotherInstanceOpen, bool isElevated)
    {
        InitializeComponent();

        // MessageBox for admin and single instance check
        if (isAnotherInstanceOpen)
        {
            MessageBox.Show("Another instance of this application is already open", "Error", MessageBoxButtons.OK);
            Environment.Exit(1);
        }
        if (!isElevated)
        {
            MessageBox.Show("Application does not have administrator privledges", "Warning", MessageBoxButtons.OK);
            DeployCheckbox.Enabled = false;
        }

        // Fields for isElevated, settings and builder, set builder's Action delegates
        this.isElevated = isElevated;
        this.settings = new();
        this.builder = new()
        {
            ReportStatus = (text) =>
            {
                if (StatusText.InvokeRequired)
                {
                    StatusText.Invoke((MethodInvoker)delegate { StatusText.Text = text; });
                }
                else
                {
                    StatusText.Text = text;
                }
            },

            ReportProgress = (percent, append) =>
            {
                if (Progressbar.InvokeRequired)
                {
                    Progressbar.Invoke((MethodInvoker)delegate
                    {
                        if (append)
                        {
                            Progressbar.Value += percent;
                        }
                        else
                        {
                            Progressbar.Value = percent;
                        }
                    });
                }
                else
                {
                    Progressbar.Value = percent;
                }
            }
        };
    }

    private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Kill any procs left over on application close
        Utils.KillRmProcs();
    }

    private void PafDataBrowseButton_Click(object sender, EventArgs e)
    {
        // Dialog to select folder for PAF data
        FolderBrowserDialog folderBrowser = new();
        DialogResult result = folderBrowser.ShowDialog();
        if (result == DialogResult.OK)
        {
            PafDataTextbox.Text = folderBrowser.SelectedPath;
        }
    }

    private void SmiDataBrowseButton_Click(object sender, EventArgs e)
    {
        // Dialog to select folder for SMi data
        FolderBrowserDialog folderBrowser = new();
        DialogResult result = folderBrowser.ShowDialog();
        if (result == DialogResult.OK)
        {
            SmiDataTextbox.Text = folderBrowser.SelectedPath;
        }
    }

    private async void BuildDirectoryButton_ClickAsync(object sender, EventArgs e)
    {
        // Disable buttons in UI, populate settings with UI inputs and pass to builder
        BuildDirectoryButton.Enabled = false;
        DeployCheckbox.Enabled = false;

        settings.PafFilesPath = PafDataTextbox.Text;
        settings.SmiFilesPath = SmiDataTextbox.Text;
        builder.Settings = settings;

        // Perform settings check first and separately, missing files are caught and handled separate from other potential issues
        try
        {
            settings.CheckPaths();
            settings.CheckMissingPafFiles();
            settings.CheckMissingSmiFiles();
            settings.CheckMissingToolFiles();
        }
        catch (FileNotFoundException error)
        {
            StatusText.Text = "Missing SMi data files or build tools";
            MessageBox.Show(error.Message, "Missing files", MessageBoxButtons.OK);
            return;
        }
        catch (Exception error)
        {
            StatusText.Text = "Error - " + error.Message;

            BuildDirectoryButton.Enabled = true;
            if (isElevated)
            {
                DeployCheckbox.Enabled = true;
            }
            return;
        }

        // Main builder procedure
        try
        {
            Progressbar.Value = 0;

            await Task.Run(async () =>
            {
                builder.Cleanup(clearOutput: true);
                builder.ConvertPafData();
                await builder.Compile();
                await builder.Output(deployToAp: DeployCheckbox.Checked);
                builder.Cleanup(clearOutput: false);
            });

            StatusText.Text = "** Directory build complete **";
            BuildDirectoryButton.Enabled = true;
            if (isElevated)
            {
                DeployCheckbox.Enabled = true;
            }
        }
        catch (Exception error)
        {
            StatusText.Text = "Error - Check log for details";
            MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK);
        }
    }
}
