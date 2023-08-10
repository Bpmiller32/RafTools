namespace Server.Common;

public class SettingsValidator
{
    public string Directory { get; set; }
    public string AddressDataPath { get; set; }
    public bool CrawlerEnabled { get; set; }
    public bool AutoCrawlEnabled { get; set; }

    public string UserName { get; set; }
    public string Password { get; set; }

    public int ExecYear { get; set; }
    public int ExecMonth { get; set; }
    public int ExecDay { get; set; }
    public int ExecHour { get; set; }
    public int ExecMinute { get; set; }
    public int ExecSecond { get; set; }

    public void Validate(IConfiguration config)
    {
        if (string.IsNullOrEmpty(config.GetValue<string>($"{Directory}:AddressDataPath")))
        {
            AddressDataPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Downloads", Directory);
        }
        else
        {
            AddressDataPath = config.GetValue<string>($"{Directory}:AddressDataPath");
            AddressDataPath = Path.GetFullPath(AddressDataPath);
        }
        if (config.GetValue<bool>($"{Directory}:CrawlerEnabled"))
        {
            CrawlerEnabled = true;
        }
        if (config.GetValue<bool>($"{Directory}:AutoCrawlEnabled"))
        {
            AutoCrawlEnabled = true;
        }

        // Time checks
        if (config.GetValue<int>($"{Directory}:ExecTime:Year") != 0)
        {
            ExecYear = config.GetValue<int>($"{Directory}:ExecTime:Year");
        }
        else
        {
            ExecYear = DateTime.Now.Year;
        }
        if (config.GetValue<int>($"{Directory}:ExecTime:Month") != 0)
        {
            ExecMonth = config.GetValue<int>($"{Directory}:ExecTime:Month");
        }
        else
        {
            ExecMonth = DateTime.Now.Month;
        }
        if (config.GetValue<int>($"{Directory}:ExecTime:Day") != 0)
        {
            ExecDay = config.GetValue<int>($"{Directory}:ExecTime:Day");
        }
        else
        {
            ExecDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        }
        if (config.GetValue<int>($"{Directory}:ExecTime:Hour") != 0)
        {
            ExecHour = config.GetValue<int>($"{Directory}:ExecTime:Hour");
        }
        else
        {
            ExecHour = 15;
        }
        if (config.GetValue<int>($"{Directory}:ExecTime:Minute") != 0)
        {
            ExecMinute = config.GetValue<int>($"{Directory}:ExecTime:Minute");
        }
        else
        {
            ExecMinute = 15;
        }
        if (config.GetValue<int>($"{Directory}:ExecTime:Second") != 0)
        {
            ExecSecond = config.GetValue<int>($"{Directory}:ExecTime:Second");
        }
        else
        {
            ExecSecond = 15;
        }

        // Check that day hasn't passed, display next month
        if (ExecDay < DateTime.Now.Day)
        {
            ExecMonth = DateTime.Now.AddMonths(1).Month;
        }

        // Login checks
        if (Directory == "SmartMatch" || Directory == "RoyalMail")
        {
            if (string.IsNullOrEmpty(config.GetValue<string>($"{Directory}:Login:User")) || string.IsNullOrEmpty(config.GetValue<string>($"{Directory}:Login:Pass")))
            {
                throw new Exception("Missing username or password for: " + Directory);
            }
            else
            {
                UserName = config.GetValue<string>($"{Directory}:Login:User");
                Password = config.GetValue<string>($"{Directory}:Login:Pass");
            }
        }
    }
}
