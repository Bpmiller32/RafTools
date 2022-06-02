namespace Common.Data;

public class ParaBundle
{
    public int Id { get; set; }

    public int DataMonth { get; set; }
    public int DataYear { get; set; }
    public int FileCount { get; set; }
    public string DataYearMonth { get; set; }
    public string DownloadDate { get; set; }
    public string DownloadTime { get; set; }
    public bool IsReadyForBuild { get; set; }
    public bool IsBuildComplete { get; set; }
    public List<ParaFile> BuildFiles { get; set; } = new List<ParaFile>();
}