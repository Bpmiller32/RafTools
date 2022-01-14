using System;

namespace Crawler.Data
{
    public class RoyalFile
    {
        public int Id { get; set; }
        
        public string FileName { get; set; }
        public string Size { get; set; }

        public int DataMonth { get; set; }
        public int DataDay { get; set; }
        public int DataYear { get; set; }
        public bool OnDisk { get; set; }
        public DateTime DateDownloaded { get; set; }
    }
}
