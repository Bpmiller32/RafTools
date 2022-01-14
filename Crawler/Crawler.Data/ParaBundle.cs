using System.Collections.Generic;

namespace Crawler.Data
{
    public class ParaBundle
    {
        public int Id { get; set; }
        
        public string DataMonth { get; set; }
        public string DataYear { get; set; }
        public bool IsReadyForBuild { get; set; }
        public List<ParaFile> BuildFiles { get; set; } = new List<ParaFile>();
    }
}
