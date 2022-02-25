using System;
using System.Collections.Generic;

namespace Crawler.Data
{
    public class SocketResponse
    {
        public CrawlStatus Status { get; set; }
        public int Progress { get; set; }
        public List<string> AvailableBuilds { get; set; }
    }
}
