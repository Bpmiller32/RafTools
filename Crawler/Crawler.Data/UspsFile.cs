using System;

namespace Crawler.Data
{
    public class UspsFile
    {
        public int Id { get; set; }
        
        // Data pulled from website
        public string FileName { get; set; }
        public bool Downloaded { get; set; }
        public DateTime UploadDate { get; set; }
        public string Size { get; set; }

        public string ProductKey { get; set; }
        public string FileId { get; set; }

        // Data relevant to build process
        public int DataMonth { get; set; }
        public int DataYear { get; set; }
        public string Cycle { get; set; }
        public bool OnDisk { get; set; }
        public DateTime DateDownloaded { get; set; }
    }
}
