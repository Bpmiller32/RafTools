namespace Server.Common;

public class ModuleSettings
{
    public string DirectoryName { get; set; }

    public string AddressDataPath { get; set; }
    public string WorkingPath { get; set; }
    public string OutputPath { get; set; }

    public string DongleListPath { get; set; }
    public string DiscDrivePath { get; set; }

    public string UserName { get; set; }
    public string Password { get; set; }

    public int ExecYear { get; set; } = DateTime.Now.Year;
    public int ExecMonth { get; set; } = DateTime.Now.Month;
    public int ExecDay { get; set; } = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
    public int ExecHour { get; set; } = 15;
    public int ExecMinute { get; set; } = 15;
    public int ExecSecond { get; set; } = 15;

    public void Validate(IConfiguration config)
    {
        // Path checks
        // Input
        if (string.IsNullOrEmpty(config.GetValue<string>($"{DirectoryName}:AddressDataPath")))
        {
            AddressDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads", DirectoryName);
        }
        else
        {
            AddressDataPath = Path.GetFullPath(config.GetValue<string>($"{DirectoryName}:AddressDataPath"));
        }
        // Temp path for working files
        if (string.IsNullOrEmpty(config.GetValue<string>($"{DirectoryName}:WorkingPath")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Temp", DirectoryName));
            WorkingPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", DirectoryName);
        }
        else
        {
            WorkingPath = Path.GetFullPath(config.GetValue<string>($"{DirectoryName}:WorkingPath"));
        }
        // Output
        if (string.IsNullOrEmpty(config.GetValue<string>($"{DirectoryName}:OutputPath")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Output", DirectoryName));
            OutputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", DirectoryName);
        }
        else
        {
            OutputPath = Path.GetFullPath(config.GetValue<string>($"{DirectoryName}:OutputPath"));
        }
        // Dongle lists checked out from Subversion
        if (string.IsNullOrEmpty(config.GetValue<string>("DongleListPath")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DongleListPath"));
            DongleListPath = Path.Combine(Directory.GetCurrentDirectory(), "DongleListPath");
        }
        else
        {
            DongleListPath = Path.GetFullPath(config.GetValue<string>("DongleListPath"));
        }
        // DVD drive path exists and is valid
        if (string.IsNullOrEmpty(config.GetValue<string>($"{DirectoryName}:TestDrivePath")))
        {
            throw new Exception($"Test drive path for {DirectoryName} is missing in appsettings");
        }
        else
        {
            DiscDrivePath = Path.GetFullPath(config.GetValue<string>($"{DirectoryName}:TestDrivePath"));
        }

        // Check that day hasn't passed, display next month
        if (ExecDay < DateTime.Now.Day)
        {
            ExecMonth = DateTime.Now.AddMonths(1).Month;
        }

        // Login checks
        if (DirectoryName == "SmartMatch" || DirectoryName == "RoyalMail")
        {
            if (string.IsNullOrEmpty(config.GetValue<string>($"{DirectoryName}:Login:User")) || string.IsNullOrEmpty(config.GetValue<string>($"{DirectoryName}:Login:Pass")))
            {
                throw new Exception("Missing username or password for: " + DirectoryName);
            }
            else
            {
                UserName = config.GetValue<string>($"{DirectoryName}:Login:User");
                Password = config.GetValue<string>($"{DirectoryName}:Login:Pass");
            }
        }
    }

    public static TimeSpan CalculateWaitTime(ILogger logger, ModuleSettings settings)
    {
        DateTime execTime = new(DateTime.Now.Year, DateTime.Now.Month, settings.ExecDay, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
        DateTime endOfMonth = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 59, 59);
        TimeSpan waitTime = execTime - DateTime.Now;

        if (waitTime.TotalSeconds <= 0)
        {
            waitTime = endOfMonth - DateTime.Now + TimeSpan.FromSeconds(5);
            logger.LogInformation($"Pass completed, starting sleep until: {endOfMonth}");
        }
        else
        {
            logger.LogInformation($"Waiting for pass, starting sleep until: {execTime}");
        }

        return waitTime;
    }

    public static ModuleSettings SetAutoWaitTime(ILogger logger, ModuleSettings settings, string autoStartTime)
    {
        settings.ExecYear = int.Parse(autoStartTime[..4]);
        settings.ExecMonth = int.Parse(autoStartTime.Substring(4, 2));
        settings.ExecDay = int.Parse(autoStartTime.Substring(6, 2));
        settings.ExecHour = int.Parse(autoStartTime.Substring(8, 2));
        settings.ExecMinute = int.Parse(autoStartTime.Substring(10, 2));

        return settings;
    }
}
