using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Crawler.Data
{
    public class Settings
    {
        public string Name { get; set; }
        public string AddressDataPath { get; set; }
        public bool CrawlerEnabled { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }

        public int ExecDay { get; set; }
        public int ExecHour { get; set; }
        public int ExecMinute { get; set; }
        public int ExecSecond { get; set; }

        public static Settings Validate(Settings settings, IConfiguration config)
        {
            // Check that appsettings.json exists at all
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), @"appsettings.json")))
            {
                throw new Exception("appsettings.json is missing, make sure there is a valid appsettings.json file in the same directory as the application");
            }

            // Verify for each directory
            List<string> directories = new List<string>() { "SmartMatch", "Parascript", "RoyalMail", "Email" };
            foreach (string dir in directories)
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


                    if (config.GetValue<int>("settings:" + dir + ":ExecTime:Day") != 0)
                    {
                        settings.ExecDay = config.GetValue<int>("settings:" + dir + ":ExecTime:Day");
                    }
                    else
                    {
                        settings.ExecDay = 15;
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
    }
}

