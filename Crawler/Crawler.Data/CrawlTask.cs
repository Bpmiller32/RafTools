using System;
using System.Threading.Tasks;

namespace Crawler.Data
{
    public class CrawlTask
    {
        public string Name { get; set; }
        public Task Task { get; set; }
        public CrawlStatus Status { get; set; }
    }
}
