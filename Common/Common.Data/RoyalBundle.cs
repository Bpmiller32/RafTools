using System.Collections.Generic;

namespace Common.Data;

public class RoyalBundle
{
    public int Id { get; set; }

    public int DataMonth { get; set; }
    public int DataYear { get; set; }
    public string DataYearMonth { get; set; }
    public bool IsReadyForBuild { get; set; }
    public bool IsBuildComplete { get; set; }
    public List<RoyalFile> BuildFiles { get; set; } = new List<RoyalFile>();
}