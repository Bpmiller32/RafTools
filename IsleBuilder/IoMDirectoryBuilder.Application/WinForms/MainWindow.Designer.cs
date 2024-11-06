namespace IoMDirectoryBuilder.Application;

partial class MainWindow
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
        this.RafLogo = new System.Windows.Forms.PictureBox();
        this.PafDataGroupbox = new System.Windows.Forms.GroupBox();
        this.PafDataBrowseButton = new System.Windows.Forms.Button();
        this.PafDataTextbox = new System.Windows.Forms.TextBox();
        this.SmiDataGroupbox = new System.Windows.Forms.GroupBox();
        this.SmiDataBrowseButton = new System.Windows.Forms.Button();
        this.SmiDataTextbox = new System.Windows.Forms.TextBox();
        this.BuildDirectoryButton = new System.Windows.Forms.Button();
        this.Progressbar = new System.Windows.Forms.ProgressBar();
        this.StatusText = new System.Windows.Forms.Label();
        this.StatusLabel = new System.Windows.Forms.Label();
        this.AppTitleLabel = new System.Windows.Forms.Label();
        this.DeployCheckbox = new System.Windows.Forms.CheckBox();
        ((System.ComponentModel.ISupportInitialize)(this.RafLogo)).BeginInit();
        this.PafDataGroupbox.SuspendLayout();
        this.SmiDataGroupbox.SuspendLayout();
        this.SuspendLayout();
        // 
        // RafLogo
        // 
        this.RafLogo.Image = ((System.Drawing.Image)(resources.GetObject("RafLogo.Image")));
        this.RafLogo.Location = new System.Drawing.Point(35, 24);
        this.RafLogo.Name = "RafLogo";
        this.RafLogo.Size = new System.Drawing.Size(125, 125);
        this.RafLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.RafLogo.TabIndex = 0;
        this.RafLogo.TabStop = false;
        // 
        // PafDataGroupbox
        // 
        this.PafDataGroupbox.Controls.Add(this.PafDataBrowseButton);
        this.PafDataGroupbox.Controls.Add(this.PafDataTextbox);
        this.PafDataGroupbox.Location = new System.Drawing.Point(35, 169);
        this.PafDataGroupbox.Name = "PafDataGroupbox";
        this.PafDataGroupbox.Size = new System.Drawing.Size(548, 100);
        this.PafDataGroupbox.TabIndex = 5;
        this.PafDataGroupbox.TabStop = false;
        this.PafDataGroupbox.Text = "Path to extracted PAF data";
        // 
        // PafDataBrowseButton
        // 
        this.PafDataBrowseButton.Location = new System.Drawing.Point(414, 43);
        this.PafDataBrowseButton.Name = "PafDataBrowseButton";
        this.PafDataBrowseButton.Size = new System.Drawing.Size(115, 35);
        this.PafDataBrowseButton.TabIndex = 1;
        this.PafDataBrowseButton.Text = "Browse ...";
        this.PafDataBrowseButton.UseVisualStyleBackColor = true;
        this.PafDataBrowseButton.Click += new System.EventHandler(this.PafDataBrowseButton_Click);
        // 
        // PafDataTextbox
        // 
        this.PafDataTextbox.Location = new System.Drawing.Point(23, 45);
        this.PafDataTextbox.Name = "PafDataTextbox";
        this.PafDataTextbox.Size = new System.Drawing.Size(361, 31);
        this.PafDataTextbox.TabIndex = 0;
        // 
        // SmiDataGroupbox
        // 
        this.SmiDataGroupbox.Controls.Add(this.SmiDataBrowseButton);
        this.SmiDataGroupbox.Controls.Add(this.SmiDataTextbox);
        this.SmiDataGroupbox.Location = new System.Drawing.Point(35, 275);
        this.SmiDataGroupbox.Name = "SmiDataGroupbox";
        this.SmiDataGroupbox.Size = new System.Drawing.Size(548, 100);
        this.SmiDataGroupbox.TabIndex = 6;
        this.SmiDataGroupbox.TabStop = false;
        this.SmiDataGroupbox.Text = "Path to SMi build files";
        // 
        // SmiDataBrowseButton
        // 
        this.SmiDataBrowseButton.Location = new System.Drawing.Point(414, 43);
        this.SmiDataBrowseButton.Name = "SmiDataBrowseButton";
        this.SmiDataBrowseButton.Size = new System.Drawing.Size(115, 35);
        this.SmiDataBrowseButton.TabIndex = 1;
        this.SmiDataBrowseButton.Text = "Browse ...";
        this.SmiDataBrowseButton.UseVisualStyleBackColor = true;
        this.SmiDataBrowseButton.Click += new System.EventHandler(this.SmiDataBrowseButton_Click);
        // 
        // SmiDataTextbox
        // 
        this.SmiDataTextbox.Location = new System.Drawing.Point(23, 45);
        this.SmiDataTextbox.Name = "SmiDataTextbox";
        this.SmiDataTextbox.Size = new System.Drawing.Size(361, 31);
        this.SmiDataTextbox.TabIndex = 0;
        // 
        // BuildDirectoryButton
        // 
        this.BuildDirectoryButton.Location = new System.Drawing.Point(234, 405);
        this.BuildDirectoryButton.Name = "BuildDirectoryButton";
        this.BuildDirectoryButton.Size = new System.Drawing.Size(150, 50);
        this.BuildDirectoryButton.TabIndex = 7;
        this.BuildDirectoryButton.Text = "Build Directory";
        this.BuildDirectoryButton.UseVisualStyleBackColor = true;
        this.BuildDirectoryButton.Click += new System.EventHandler(this.BuildDirectoryButton_ClickAsync);
        // 
        // Progressbar
        // 
        this.Progressbar.Location = new System.Drawing.Point(35, 465);
        this.Progressbar.Name = "Progressbar";
        this.Progressbar.Size = new System.Drawing.Size(548, 20);
        this.Progressbar.TabIndex = 8;
        // 
        // StatusText
        // 
        this.StatusText.AutoSize = true;
        this.StatusText.Location = new System.Drawing.Point(246, 97);
        this.StatusText.Name = "StatusText";
        this.StatusText.Size = new System.Drawing.Size(60, 25);
        this.StatusText.TabIndex = 3;
        this.StatusText.Text = "Ready";
        // 
        // StatusLabel
        // 
        this.StatusLabel.AutoSize = true;
        this.StatusLabel.Location = new System.Drawing.Point(190, 97);
        this.StatusLabel.Name = "StatusLabel";
        this.StatusLabel.Size = new System.Drawing.Size(64, 25);
        this.StatusLabel.TabIndex = 2;
        this.StatusLabel.Text = "Status:";
        // 
        // AppTitleLabel
        // 
        this.AppTitleLabel.AutoSize = true;
        this.AppTitleLabel.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.AppTitleLabel.Location = new System.Drawing.Point(188, 50);
        this.AppTitleLabel.Name = "AppTitleLabel";
        this.AppTitleLabel.Size = new System.Drawing.Size(393, 41);
        this.AppTitleLabel.TabIndex = 1;
        this.AppTitleLabel.Text = "Isle of Man Directory Builder";
        // 
        // DeployCheckbox
        // 
        this.DeployCheckbox.AutoSize = true;
        this.DeployCheckbox.Location = new System.Drawing.Point(439, 417);
        this.DeployCheckbox.Name = "DeployCheckbox";
        this.DeployCheckbox.Size = new System.Drawing.Size(144, 29);
        this.DeployCheckbox.TabIndex = 9;
        this.DeployCheckbox.Text = "Deploy to AP";
        this.DeployCheckbox.UseVisualStyleBackColor = true;
        // 
        // MainWindow
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(618, 519);
        this.Controls.Add(this.DeployCheckbox);
        this.Controls.Add(this.AppTitleLabel);
        this.Controls.Add(this.StatusText);
        this.Controls.Add(this.Progressbar);
        this.Controls.Add(this.StatusLabel);
        this.Controls.Add(this.BuildDirectoryButton);
        this.Controls.Add(this.SmiDataGroupbox);
        this.Controls.Add(this.PafDataGroupbox);
        this.Controls.Add(this.RafLogo);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "MainWindow";
        this.Text = "Isle of Man Directory Builder 1.0";
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
        ((System.ComponentModel.ISupportInitialize)(this.RafLogo)).EndInit();
        this.PafDataGroupbox.ResumeLayout(false);
        this.PafDataGroupbox.PerformLayout();
        this.SmiDataGroupbox.ResumeLayout(false);
        this.SmiDataGroupbox.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private PictureBox RafLogo;
    private GroupBox PafDataGroupbox;
    private Button PafDataBrowseButton;
    private TextBox PafDataTextbox;
    private GroupBox SmiDataGroupbox;
    private Button SmiDataBrowseButton;
    private TextBox SmiDataTextbox;
    private Button BuildDirectoryButton;
    private ProgressBar Progressbar;
    private Label StatusText;
    private Label StatusLabel;
    private Label AppTitleLabel;
    private CheckBox DeployCheckbox;
}
