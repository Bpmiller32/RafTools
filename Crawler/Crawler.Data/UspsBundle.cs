using System.Collections.Generic;

namespace Crawler.Data
{
    public class UspsBundle
    {
        public int Id { get; set; }
        
        public int DataMonth { get; set; }
        public int DataYear { get; set; }
        public string Cycle { get; set; }
        public bool IsReadyForBuild { get; set; }
        public List<UspsFile> BuildFiles { get; set; } = new List<UspsFile>();
    }
}
