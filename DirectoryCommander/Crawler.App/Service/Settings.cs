namespace Crawler;

public class Settings
{
    public string Name { get; set; }
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

    public static Settings Validate(Settings settings, IConfiguration config)
    {
        // Check that appsettings.json exists at all
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
        {
            throw new Exception("appsettings.json is missing, make sure there is a valid appsettings.json file in the same directory as the application");
        }

        // Verify for each directory
        foreach (string dir in new List<string>() { "SmartMatch", "Parascript", "RoyalMail", "Email" })
        {
            if (dir == settings.Name)
            {
                if (string.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":AddressDataPath")))
                {
                    settings.AddressDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads", dir);
                }
                if (config.GetValue<bool>("settings:" + dir + ":CrawlerEnabled"))
                {
                    settings.CrawlerEnabled = true;
                }
                if (config.GetValue<bool>("settings:" + dir + ":AutoCrawlEnabled"))
                {
                    settings.AutoCrawlEnabled = true;
                }

                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Year") != 0)
                {
                    settings.ExecYear = config.GetValue<int>("settings:" + dir + ":ExecTime:Year");
                }
                else
                {
                    settings.ExecYear = DateTime.Now.Year;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Month") != 0)
                {
                    settings.ExecMonth = config.GetValue<int>("settings:" + dir + ":ExecTime:Month");
                }
                else
                {
                    settings.ExecMonth = DateTime.Now.Month;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Day") != 0)
                {
                    settings.ExecDay = config.GetValue<int>("settings:" + dir + ":ExecTime:Day");
                }
                else
                {
                    settings.ExecDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Hour") != 0)
                {
                    settings.ExecHour = config.GetValue<int>("settings:" + dir + ":ExecTime:Hour");
                }
                else
                {
                    settings.ExecHour = 15;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Minute") != 0)
                {
                    settings.ExecMinute = config.GetValue<int>("settings:" + dir + ":ExecTime:Minute");
                }
                else
                {
                    settings.ExecMinute = 15;
                }
                if (config.GetValue<int>("settings:" + dir + ":ExecTime:Second") != 0)
                {
                    settings.ExecSecond = config.GetValue<int>("settings:" + dir + ":ExecTime:Second");
                }
                else
                {
                    settings.ExecSecond = 15;
                }

                // Check that day hasn't passed, display next month
                if (settings.ExecDay < DateTime.Now.Day)
                {
                    settings.ExecMonth = DateTime.Now.AddMonths(1).Month;
                }

                if (settings.Name == "SmartMatch" || settings.Name == "RoyalMail" || settings.Name == "Email")
                {
                    if (String.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":Login:User")) || String.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":Login:Pass")))
                    {
                        throw new Exception("Missing username or password for: " + settings.Name);
                    }
                    else
                    {
                        settings.UserName = config.GetValue<string>("settings:" + dir + ":Login:User");
                        settings.Password = config.GetValue<string>("settings:" + dir + ":Login:Pass");
                    }
                }
            }
        }

        return settings;
    }

    public static TimeSpan CalculateWaitTime(ILogger logger, Settings settings)
    {
        DateTime execTime = new(DateTime.Now.Year, DateTime.Now.Month, settings.ExecDay, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
        DateTime endOfMonth = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 59, 59);
        TimeSpan waitTime = execTime - DateTime.Now;

        if (waitTime.TotalSeconds <= 0)
        {
            waitTime = endOfMonth - DateTime.Now + TimeSpan.FromSeconds(5);
            logger.LogInformation("Pass completed, starting sleep until: {endOfMonth}", endOfMonth);
        }
        else
        {
            logger.LogInformation("Waiting for pass, starting sleep until: {execTime}", execTime);
        }

        return waitTime;
    }

    public static DateTime CalculateNextDate(Settings settings)
    {
        DateTime execTime = new(DateTime.Now.Year, DateTime.Now.Month, settings.ExecDay, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
        DateTime endOfMonth = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 59, 59);
        TimeSpan waitTime = execTime - DateTime.Now;

        if (waitTime.TotalSeconds <= 0)
        {
            return endOfMonth;
        }

        return execTime;
    }
}