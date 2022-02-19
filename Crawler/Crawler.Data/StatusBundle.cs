using System;
using System.Collections.Generic;

namespace Crawler.Data
{
    public class StatusBundle
    {
        public CrawlStatus Status { get; set; }
        public List<string> AvailableBuilds { get; set; }
    }
}
