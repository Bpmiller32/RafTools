using System;

namespace Crawler.Data
{
    public class CrawlTask
    {
        public CrawlStatus Email { get; set; }
        public CrawlStatus SmartMatch { get; set; }
        public CrawlStatus Parascript { get; set; }
        public CrawlStatus RoyalMail { get; set; }
    }
}
