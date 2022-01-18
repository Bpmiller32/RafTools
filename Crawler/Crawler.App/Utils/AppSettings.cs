using System.IO;

namespace Crawler.Data
{
    public class AppSettings
    {
        public bool ServiceEnabled { get; set; } = true;
        
        public string DownloadPath { get; set; } = Directory.GetCurrentDirectory() + @"\Downloads";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        
        public int ExecDay { get; set; } = 15;
        public int ExecHour { get; set; } = 7;
        public int ExecMinute { get; set; } = 59;
        public int ExecSecond { get; set; } = 59;
    }
}

